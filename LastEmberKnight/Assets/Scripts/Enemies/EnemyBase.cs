using System;
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    // EnemyType enum is defined in GameTypes.cs

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        // ── Inspector Fields ──────────────────────────────────────────────
        [Header("Stats")]
        [SerializeField] protected float maxHP = 30f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float moveSpeed = 2f;
        [SerializeField] protected float knockbackResistance = 0f;

        [Header("Invincibility")]
        [SerializeField] protected float invincibilityDuration = 0.2f;

        [Header("Soul Drop")]
        [SerializeField] protected int minShardDrop = 1;
        [SerializeField] protected int maxShardDrop = 3;

        [Header("VFX")]
        [SerializeField] protected GameObject deathParticlesPrefab;
        [SerializeField] protected GameObject hitParticlesPrefab;

        // ── Runtime State ──────────────────────────────────────────────────
        protected float currentHP;
        protected bool isInvincible;
        protected bool isDead;
        protected Rigidbody2D rb;
        protected Collider2D col;
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        protected Transform playerTransform;

        // ── Events ─────────────────────────────────────────────────────────
        public static event Action<EnemyBase> OnAnyEnemyDeath;
        public event Action OnDeath;
        public event Action<float> OnDamageReceived;

        // ── IDamageable ────────────────────────────────────────────────────
        public float HP => currentHP;
        public float MaxHP => maxHP;

        // ── Properties ─────────────────────────────────────────────────────
        public bool IsAlive => !isDead && currentHP > 0f;

        public bool IsFacingPlayer
        {
            get
            {
                if (playerTransform == null) return false;
                float dir = playerTransform.position.x - transform.position.x;
                float facing = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
                return Mathf.Sign(dir) == Mathf.Sign(facing);
            }
        }

        public float DistanceToPlayer
        {
            get
            {
                if (playerTransform == null) return float.MaxValue;
                return Vector2.Distance(transform.position, playerTransform.position);
            }
        }

        // ── Unity Lifecycle ────────────────────────────────────────────────
        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            ApplyDifficultyScaling();

            currentHP = maxHP;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        protected virtual void Update()
        {
            if (!IsAlive || _isStunned) return;
            UpdateAI();
        }

        // ── Difficulty Scaling ─────────────────────────────────────────────
        protected virtual void ApplyDifficultyScaling()
        {
            if (GameManager.Instance == null) return;

            float mult = GameManager.Instance.Difficulty;
            maxHP *= mult;
            damage *= mult;
        }

        // ── Damage / Death ─────────────────────────────────────────────────
        public virtual void TakeDamage(float amount, Vector2 knockback)
        {
            if (!IsAlive || isInvincible) return;

            float finalAmount = Mathf.Max(0f, amount);
            currentHP -= finalAmount;

            OnDamageReceived?.Invoke(finalAmount);
            OnHit();

            // Knockback
            if (rb != null)
            {
                Vector2 kbForce = knockback * (1f - Mathf.Clamp01(knockbackResistance));
                rb.AddForce(kbForce, ForceMode2D.Impulse);
            }

            // Hit particles
            if (hitParticlesPrefab != null)
                Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);

            if (currentHP <= 0f)
            {
                Die();
                return;
            }

            StartCoroutine(InvincibilityCoroutine());
        }

        protected IEnumerator InvincibilityCoroutine()
        {
            isInvincible = true;

            // Blink effect
            if (spriteRenderer != null)
            {
                float elapsed = 0f;
                while (elapsed < invincibilityDuration)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                    yield return new WaitForSeconds(0.05f);
                    elapsed += 0.05f;
                }
                spriteRenderer.enabled = true;
            }
            else
            {
                yield return new WaitForSeconds(invincibilityDuration);
            }

            isInvincible = false;
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            // Drop shards
            DropShards();

            // Death particles
            if (deathParticlesPrefab != null)
                Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);

            OnDeath?.Invoke();
            OnAnyEnemyDeath?.Invoke(this);
            HandleOnDeath();

            Destroy(gameObject, 0.05f);
        }

        void DropShards()
        {
            int shardCount = UnityEngine.Random.Range(minShardDrop, maxShardDrop + 1);
            // Notify progression / shard system if present
            if (ShardManager.Instance != null)
                ShardManager.Instance.SpawnShards(transform.position, shardCount);
        }

        // ── Abstract / Virtual Hooks ───────────────────────────────────────
        protected abstract void UpdateAI();

        // Called just before the GameObject is destroyed on death
        protected virtual void HandleOnDeath() { }

        protected abstract void OnHit();

        // ── Facing Helper ──────────────────────────────────────────────────
        protected void FaceDirection(float horizontalDir)
        {
            if (spriteRenderer == null) return;
            if (horizontalDir > 0.01f) spriteRenderer.flipX = false;
            else if (horizontalDir < -0.01f) spriteRenderer.flipX = true;
        }

        // ── Parry / Stun Support ───────────────────────────────────────────

        // Tracks the enemy's current "startup" frame counter.
        // Set by each concrete AI when it begins an attack wind-up.
        protected int startupFrameCounter = 0;
        private bool  _isStunned;
        private Coroutine _stunCoroutine;

        /// <summary>
        /// Returns true while the enemy is in its attack startup window,
        /// meaning the player can land a parry (valid for frames 0 to maxFrames).
        /// </summary>
        public bool IsInStartupFrames(int maxFrames) => startupFrameCounter > 0 && startupFrameCounter <= maxFrames;

        /// <summary>
        /// Stun this enemy for <paramref name="duration"/> seconds.
        /// While stunned <see cref="UpdateAI"/> is not called.
        /// </summary>
        public void ApplyStun(float duration)
        {
            if (_stunCoroutine != null) StopCoroutine(_stunCoroutine);
            _stunCoroutine = StartCoroutine(StunCoroutine(duration));
        }

        private IEnumerator StunCoroutine(float duration)
        {
            _isStunned = true;
            yield return new WaitForSeconds(duration);
            _isStunned = false;
            _stunCoroutine = null;
        }

    }
}
