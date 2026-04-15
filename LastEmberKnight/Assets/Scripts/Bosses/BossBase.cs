using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastEmberKnight
{
    /// <summary>
    /// Abstract base class for all bosses in The Last Ember Knight.
    /// Provides health, phase transitions, invincibility frames, difficulty scaling,
    /// death handling, and UI health bar integration.
    /// Boss levels: 4, 9, 14, 19, 24, 29, 34, 39, 44, 49.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class BossBase : MonoBehaviour, IDamageable
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Boss Identity")]
        [SerializeField] protected string bossName = "Unknown Boss";
        [SerializeField] protected int bossIndex = 0;
        [SerializeField] protected bool isElderFlag = false;

        [Header("Stats")]
        [SerializeField] protected float maxHP = 300f;
        [SerializeField] protected float damage = 25f;

        [Header("Invincibility")]
        [SerializeField] protected float invincibilityDuration = 0.3f;

        [Header("Phase Transition")]
        [SerializeField] protected float phaseTransitionInvincibility = 1.5f;

        [Header("Death - Shard Drop")]
        [SerializeField] protected int minShardDrop = 20;
        [SerializeField] protected int maxShardDrop = 30;

        [Header("VFX")]
        [SerializeField] protected GameObject deathParticlesPrefab;
        [SerializeField] protected GameObject hitParticlesPrefab;
        [SerializeField] protected GameObject phaseTransitionParticlesPrefab;

        [Header("UI")]
        [SerializeField] protected BossHealthBar bossHealthBar;

        // ── Runtime State ──────────────────────────────────────────────────

        protected float currentHP;
        protected int phase = 1;
        protected bool isInvincible = false;
        protected bool isDead = false;
        protected bool phaseTransitioned = false;

        protected Rigidbody2D rb;
        protected Collider2D col;
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        protected Transform playerTransform;

        // ── IDamageable ────────────────────────────────────────────────────

        public float HP    => currentHP;
        public float MaxHP => maxHP;

        // ── Events ─────────────────────────────────────────────────────────

        /// <summary>Fired when this boss is defeated. Carries the boss index.</summary>
        public static event Action<BossBase> OnBossDefeated;

        public event Action OnDeath;
        public event Action<float> OnDamageReceived;
        public event Action<int> OnPhaseChanged;

        // ── Properties ─────────────────────────────────────────────────────

        public int Phase         => phase;
        public int BossIndex     => bossIndex;
        public bool IsAlive      => !isDead && currentHP > 0f;
        public bool IsElderFlag  => isElderFlag;

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
            rb            = GetComponent<Rigidbody2D>();
            col           = GetComponent<Collider2D>();
            animator      = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            ApplyDifficultyScaling();
            currentHP = maxHP;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;

            if (bossHealthBar != null)
                bossHealthBar.Initialise(GetDisplayName(), maxHP, isElderFlag);

            phase = 1;
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;

            CheckPhaseTransition();
            UpdateBossHealthBar();
        }

        // ── Difficulty Scaling ─────────────────────────────────────────────

        protected virtual void ApplyDifficultyScaling()
        {
            if (GameManager.Instance == null) return;
            float mult = GameManager.Instance.Difficulty;
            maxHP   *= mult;
            damage  *= mult;
        }

        // ── Health / Damage ────────────────────────────────────────────────

        public virtual void TakeDamage(float amount, Vector2 knockback)
        {
            if (!IsAlive || isInvincible) return;

            float finalAmount = Mathf.Max(0f, amount);
            currentHP -= finalAmount;
            currentHP  = Mathf.Max(0f, currentHP);

            OnDamageReceived?.Invoke(finalAmount);
            OnHitReaction();

            if (rb != null && knockback != Vector2.zero)
                rb.AddForce(knockback, ForceMode2D.Impulse);

            if (hitParticlesPrefab != null)
                Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);

            if (currentHP <= 0f)
            {
                Die();
                return;
            }

            StartCoroutine(InvincibilityCoroutine(invincibilityDuration));
        }

        // ── Phase Transition ───────────────────────────────────────────────

        private void CheckPhaseTransition()
        {
            if (phaseTransitioned || phase != 1) return;
            if (currentHP <= maxHP * 0.5f)
            {
                phaseTransitioned = true;
                phase = 2;
                StartCoroutine(PhaseTransitionSequence());
            }
        }

        private IEnumerator PhaseTransitionSequence()
        {
            // Become invincible during transition
            isInvincible = true;

            OnPhaseChanged?.Invoke(2);

            // Let BossPhaseManager handle visuals if present
            BossPhaseManager phaseManager = GetComponentInChildren<BossPhaseManager>();
            if (phaseManager != null)
                phaseManager.TriggerPhaseTransition(this);

            // Particle burst
            if (phaseTransitionParticlesPrefab != null)
                Instantiate(phaseTransitionParticlesPrefab, transform.position, Quaternion.identity);

            // Boss-specific phase logic
            OnPhaseTransition();

            yield return new WaitForSeconds(phaseTransitionInvincibility);
            isInvincible = false;
        }

        // ── Death ──────────────────────────────────────────────────────────

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            StopAllCoroutines();

            DropShards();
            SpawnDramaticDeathParticles();

            if (bossHealthBar != null)
                bossHealthBar.Hide();

            GameManager.Instance?.NotifyBossDefeated();
            OnBossDefeated?.Invoke(this);
            OnDeath?.Invoke();

            HandleOnBossDefeated();

            Destroy(gameObject, 2f);
        }

        private void DropShards()
        {
            int count = UnityEngine.Random.Range(minShardDrop, maxShardDrop + 1);
            if (ShardManager.Instance != null)
                ShardManager.Instance.SpawnShards(transform.position, count);
        }

        private void SpawnDramaticDeathParticles()
        {
            if (deathParticlesPrefab == null) return;

            // Spawn multiple bursts at offsets for a dramatic effect
            Vector3[] offsets = new Vector3[]
            {
                Vector3.zero,
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0f,    1.0f, 0f),
            };

            foreach (Vector3 offset in offsets)
            {
                GameObject fx = Instantiate(deathParticlesPrefab,
                    transform.position + offset,
                    Quaternion.identity);
                // Auto-destroy the extra particles after a few seconds
                Destroy(fx, 4f);
            }
        }

        // ── UI ─────────────────────────────────────────────────────────────

        private void UpdateBossHealthBar()
        {
            if (bossHealthBar != null)
                bossHealthBar.SetHP(currentHP, maxHP);
        }

        // ── Invincibility Coroutine ────────────────────────────────────────

        protected IEnumerator InvincibilityCoroutine(float duration)
        {
            isInvincible = true;

            if (spriteRenderer != null)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                    yield return new WaitForSeconds(0.05f);
                    elapsed += 0.05f;
                }
                spriteRenderer.enabled = true;
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }

            isInvincible = false;
        }

        // ── Facing Helper ──────────────────────────────────────────────────

        protected void FacePlayer()
        {
            if (playerTransform == null || spriteRenderer == null) return;
            float dir = playerTransform.position.x - transform.position.x;
            spriteRenderer.flipX = dir < 0f;
        }

        protected void FaceDirection(float horizontalDir)
        {
            if (spriteRenderer == null) return;
            if (horizontalDir > 0.01f)       spriteRenderer.flipX = false;
            else if (horizontalDir < -0.01f) spriteRenderer.flipX = true;
        }

        // ── Abstract Interface ─────────────────────────────────────────────

        /// <summary>Called each frame to execute the boss's attack pattern.</summary>
        protected abstract void ExecuteAttack();

        /// <summary>Called exactly once when HP drops to or below 50%.</summary>
        protected abstract void OnPhaseTransition();

        /// <summary>Returns the display name shown in the health bar UI.</summary>
        public abstract string GetDisplayName();

        /// <summary>Called when a hit lands (not on death). Override for hit-reaction logic.</summary>
        protected virtual void OnHitReaction() { }

        /// <summary>Called after all death cleanup. Override for boss-specific defeat logic.</summary>
        protected virtual void HandleOnBossDefeated() { }

        // ── Gizmos ────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
#endif
    }
}
