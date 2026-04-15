using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 6 – Obsidian Sentinel (Zone 6, Level 34).
    /// A colossal golem who grows larger and more dangerous over time.
    /// 550 HP / 32 damage / 38x44 size.
    /// Phase 2 "Grow": scale increases 1.4x; punches create ground shockwaves.
    /// 25% chance when hit in melee to briefly stun THE PLAYER.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss6ObsidianSentinel : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Obsidian Sentinel – Stats")]
        [SerializeField] private float moveSpeed = 2.0f;

        [Header("Grow")]
        [SerializeField] private float growScaleMultiplier = 1.4f;

        [Header("Avalanche")]
        [SerializeField] private int rockCount = 3;
        [SerializeField] private GameObject rockProjectilePrefab;
        [SerializeField] private float rockSpeed = 6f;
        [SerializeField] private float rockBounceCount = 2;
        [SerializeField] private float avalancheCooldown = 6f;
        [SerializeField] private float rockSpreadX = 2.5f;

        [Header("Toss")]
        [SerializeField] private GameObject platformChunkPrefab;
        [SerializeField] private float tossSpeed = 9f;
        [SerializeField] private float tossCooldown = 5f;
        [SerializeField] private Vector2 tossHitboxSize = new Vector2(2.5f, 2.5f);

        [Header("Ground Shockwave (Phase 2)")]
        [SerializeField] private GameObject shockwavePrefab;
        [SerializeField] private float shockwaveWidth = 6f;
        [SerializeField] private float shockwaveHeight = 1.2f;

        [Header("Counter Stun")]
        [SerializeField] private float counterStunChance = 0.25f;
        [SerializeField] private float counterStunDuration = 0.35f;
        [SerializeField] private float counterStunMeleeRange = 2.5f;

        [Header("Melee")]
        [SerializeField] private float meleeRange = 2.0f;
        [SerializeField] private float meleeCooldown = 1.8f;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.20f, 0.20f, 0.22f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.10f, 0.10f, 0.12f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _avalancheTimer;
        private float _tossTimer;
        private float _meleeTimer;
        private bool _isActing;
        private bool _hasGrown;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Obsidian Sentinel";

        protected override void OnPhaseTransition()
        {
            if (!_hasGrown)
            {
                _hasGrown = true;
                // Grow
                transform.localScale *= growScaleMultiplier;
            }

            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[ObsidianSentinel] Phase 2 – GROW activated! Now larger and creating shockwaves.");
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Obsidian Sentinel";
            bossIndex   = 6;
            maxHP       = 550f;
            damage      = 32f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(38f / 32f, 44f / 32f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _avalancheTimer -= Time.deltaTime;
            _tossTimer      -= Time.deltaTime;
            _meleeTimer     -= Time.deltaTime;

            float dist = DistanceToPlayer;

            if (_avalancheTimer <= 0f)
            {
                StartCoroutine(PerformAvalanche());
                return;
            }

            if (_tossTimer <= 0f && dist > meleeRange)
            {
                StartCoroutine(PerformToss());
                return;
            }

            if (_meleeTimer <= 0f && dist <= meleeRange)
            {
                StartCoroutine(PerformMeleePunch());
                return;
            }

            if (dist > meleeRange)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Counter Stun (OnHitReaction) ───────────────────────────────────

        protected override void OnHitReaction()
        {
            // 25% chance to briefly stun the player when hit from melee range
            if (playerTransform == null) return;
            if (DistanceToPlayer > counterStunMeleeRange) return;
            if (Random.value > counterStunChance) return;

            PlayerMovementDebuff stun = playerTransform.GetComponent<PlayerMovementDebuff>();
            if (stun == null) stun = playerTransform.gameObject.AddComponent<PlayerMovementDebuff>();
            stun.ApplyStun(counterStunDuration);

            Debug.Log("[ObsidianSentinel] Counter reaction – player briefly stunned!");
        }

        // ── Attack 1: Avalanche ────────────────────────────────────────────

        private IEnumerator PerformAvalanche()
        {
            _isActing       = true;
            _avalancheTimer = avalancheCooldown;

            if (animator != null) animator.SetTrigger("Avalanche");

            yield return new WaitForSeconds(0.4f);

            // Spawn 3 rocks from above, aimed at player position with spread
            Vector2 targetBase = playerTransform != null
                ? (Vector2)playerTransform.position
                : (Vector2)transform.position;

            for (int i = 0; i < rockCount; i++)
            {
                float offsetX = (i - rockCount / 2) * rockSpreadX;
                Vector3 spawnPos = new Vector3(targetBase.x + offsetX,
                    transform.position.y + 5f, 0f);
                Vector2 target = targetBase + new Vector2(offsetX, 0f);

                SpawnBouncingRock(spawnPos, target);
                yield return new WaitForSeconds(0.12f);
            }

            yield return new WaitForSeconds(0.8f);
            _isActing = false;
        }

        private void SpawnBouncingRock(Vector3 spawnPos, Vector2 target)
        {
            if (rockProjectilePrefab == null) return;

            GameObject rock = Instantiate(rockProjectilePrefab, spawnPos, Quaternion.identity);
            BouncingRock rockScript = rock.GetComponent<BouncingRock>();
            if (rockScript == null) rockScript = rock.AddComponent<BouncingRock>();
            rockScript.Initialise(target, rockSpeed, (int)rockBounceCount, damage * 0.85f,
                playerLayer, gameObject);
        }

        // ── Attack 2: Toss ─────────────────────────────────────────────────

        private IEnumerator PerformToss()
        {
            _isActing  = true;
            _tossTimer = tossCooldown;

            if (animator != null) animator.SetTrigger("Toss");

            yield return new WaitForSeconds(0.35f);

            if (playerTransform != null && platformChunkPrefab != null)
            {
                Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                GameObject chunk = Instantiate(platformChunkPrefab,
                    (Vector2)transform.position + dir * 1.2f, Quaternion.identity);

                TossProjectile tossScript = chunk.GetComponent<TossProjectile>();
                if (tossScript == null) tossScript = chunk.AddComponent<TossProjectile>();
                tossScript.Initialise(dir, tossSpeed, tossHitboxSize, damage * 1.1f,
                    playerLayer, gameObject);
            }

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        // ── Melee Punch ────────────────────────────────────────────────────

        private IEnumerator PerformMeleePunch()
        {
            _isActing   = true;
            _meleeTimer = meleeCooldown;

            if (animator != null) animator.SetTrigger("Punch");
            yield return new WaitForSeconds(0.25f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX * 1.1f;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(2f, 1.8f), 0f, playerLayer);

            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(facingX * 5f, 3f), DamageType.Normal, gameObject);
            }

            // Phase 2: ground shockwave propagates after punch
            if (phase == 2)
                SpawnGroundShockwave(facingX);

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        private void SpawnGroundShockwave(float dirX)
        {
            if (shockwavePrefab == null) return;

            Vector3 wavePos = transform.position + Vector3.right * dirX * shockwaveWidth * 0.5f
                              + Vector3.down * 0.5f;
            GameObject wave = Instantiate(shockwavePrefab, wavePos, Quaternion.identity);
            wave.transform.localScale = new Vector3(shockwaveWidth, shockwaveHeight, 1f);

            // Damage anything in shockwave
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                (Vector2)wavePos, new Vector2(shockwaveWidth, shockwaveHeight), 0f, playerLayer);
            foreach (Collider2D c in hits)
            {
                IDamageable dmg = c.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage * 0.7f,
                        new Vector2(dirX * 4f, 5f), DamageType.Normal, gameObject);
            }

            Destroy(wave, 0.3f);
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, meleeRange);
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, counterStunMeleeRange);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Rock projectile that bounces off the ground a set number of times.
    /// </summary>
    public class BouncingRock : MonoBehaviour
    {
        private Vector2 _target;
        private float _speed;
        private int _bouncesLeft;
        private float _damage;
        private LayerMask _playerLayer;
        private GameObject _owner;

        private Rigidbody2D _rb;
        private bool _launched;
        private HashSet<Collider2D> _hitSet = new HashSet<Collider2D>();

        public void Initialise(Vector2 target, float speed, int bounces, float damage,
            LayerMask playerLayer, GameObject owner)
        {
            _target      = target;
            _speed       = speed;
            _bouncesLeft = bounces;
            _damage      = damage;
            _playerLayer = playerLayer;
            _owner       = owner;

            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();

            // Launch toward target
            Vector2 dir = (_target - (Vector2)transform.position).normalized;
            _rb.velocity = dir * _speed;
            _launched = true;

            Destroy(gameObject, 5f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_launched) return;

            // Bounce off ground
            int otherLayer = 1 << other.gameObject.layer;
            if (((1 << LayerMask.NameToLayer(GameConstants.LayerGround)) & otherLayer) != 0)
            {
                if (_bouncesLeft > 0)
                {
                    _bouncesLeft--;
                    if (_rb != null)
                        _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Abs(_rb.velocity.y) * 0.8f);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            // Hit player
            if ((otherLayer & _playerLayer.value) == 0 || _hitSet.Contains(other)) return;
            _hitSet.Add(other);

            IDamageable dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
                DamageSystem.Instance?.DealDamage(dmg, _damage, Vector2.up * 3f,
                    DamageType.Normal, _owner);
        }
    }

    /// <summary>
    /// Large platform chunk projectile thrown by the Obsidian Sentinel.
    /// </summary>
    public class TossProjectile : MonoBehaviour
    {
        private Vector2 _dir;
        private float _speed;
        private Vector2 _hitboxSize;
        private float _damage;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private bool _hit;

        public void Initialise(Vector2 dir, float speed, Vector2 hitboxSize, float damage,
            LayerMask playerLayer, GameObject owner)
        {
            _dir         = dir;
            _speed       = speed;
            _hitboxSize  = hitboxSize;
            _damage      = damage;
            _playerLayer = playerLayer;
            _owner       = owner;

            Destroy(gameObject, 4f);
        }

        private void Update()
        {
            if (_hit) return;
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
            transform.Rotate(0f, 0f, 180f * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hit) return;
            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) == 0) return;

            _hit = true;
            IDamageable dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
                DamageSystem.Instance?.DealDamage(dmg, _damage,
                    _dir * 4f + Vector2.up * 3f, DamageType.Normal, _owner);

            Destroy(gameObject);
        }
    }
}
