using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 4 – The Flood (Zone 4, Level 24).
    /// A liquid entity that morphs between forms to attack.
    /// 450 HP / 28 damage / 36x36 size.
    /// Phase 2: water clone mirrors attacks from the other side of the arena,
    /// and the arena floor floods periodically (3s damage zone at the bottom).
    /// Visual: liquid blue/teal, fluid movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss4TheFlood : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("The Flood – Stats")]
        [SerializeField] private float moveSpeed = 4f;

        [Header("Waveform Dash")]
        [SerializeField] private float waveformSpeed = 20f;
        [SerializeField] private float waveformDamage = 1.2f; // multiplier of base damage
        [SerializeField] private float waveformCooldown = 5f;
        [SerializeField] private GameObject waveformTrailPrefab;

        [Header("Adaptive Strike")]
        [SerializeField] private float adaptiveRangeThreshold = 4f;
        [SerializeField] private float rangedBoltSpeed = 8f;
        [SerializeField] private GameObject waterBoltPrefab;
        [SerializeField] private float adaptiveCooldown = 2.5f;

        [Header("Phase 2 – Clone")]
        [SerializeField] private GameObject waterClonePrefab;
        [SerializeField] private float cloneOffset = 8f;
        [SerializeField] private float floodInterval = 6f;
        [SerializeField] private float floodDuration = 3f;
        [SerializeField] private GameObject floodZonePrefab;
        [SerializeField] private float floodZoneHeight = 1.5f;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.10f, 0.55f, 0.80f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.05f, 0.75f, 0.95f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _waveformTimer;
        private float _adaptiveTimer;
        private float _floodTimer;
        private bool _isActing;
        private WaterCloneController _clone;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "The Flood";

        protected override void OnPhaseTransition()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            SpawnWaterClone();
            _floodTimer = floodInterval;

            Debug.Log("[TheFlood] Phase 2 – Clone summoned, arena flooding begins!");
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "The Flood";
            bossIndex   = 4;
            maxHP       = 450f;
            damage      = 28f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(36f / 32f, 36f / 32f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _waveformTimer -= Time.deltaTime;
            _adaptiveTimer -= Time.deltaTime;

            if (phase == 2)
            {
                _floodTimer -= Time.deltaTime;
                if (_floodTimer <= 0f)
                {
                    _floodTimer = floodInterval;
                    StartCoroutine(FloodArenaBottom());
                }
            }

            if (_waveformTimer <= 0f)
            {
                StartCoroutine(PerformWaveform());
                return;
            }

            if (_adaptiveTimer <= 0f)
            {
                StartCoroutine(PerformAdaptiveStrike());
                return;
            }

            if (DistanceToPlayer > 1.5f)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Attack 1: Waveform ─────────────────────────────────────────────

        /// <summary>
        /// Dashes across the entire arena as a wave of water, damaging everything in path.
        /// </summary>
        private IEnumerator PerformWaveform()
        {
            _isActing      = true;
            _waveformTimer = waveformCooldown;

            if (animator != null) animator.SetTrigger("Waveform");

            Rigidbody2D bossRb = GetComponent<Rigidbody2D>();
            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;

            // Telegraph
            yield return StartCoroutine(_ai.TelegraphAttack(0.35f, transform.position));

            // Full arena dash
            float arenaEndX = facingX > 0f ? _ai.ArenaRight - 1f : _ai.ArenaLeft + 1f;
            float dashTime  = Mathf.Abs(arenaEndX - transform.position.x) / waveformSpeed;

            // Become intangible briefly (still collidable)
            isInvincible = true;

            // Spawn trail
            GameObject trail = null;
            if (waveformTrailPrefab != null)
            {
                trail = Instantiate(waveformTrailPrefab, transform.position, Quaternion.identity);
                trail.transform.localScale = new Vector3(Mathf.Abs(arenaEndX - transform.position.x), 1.5f, 1f);
                trail.transform.position = new Vector3(
                    (transform.position.x + arenaEndX) * 0.5f,
                    transform.position.y, 0f);
            }

            float elapsed = 0f;
            HashSet<Collider2D> hit = new HashSet<Collider2D>();

            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                transform.position += Vector3.right * facingX * waveformSpeed * Time.deltaTime;

                // Damage player in path
                Collider2D playerHit = Physics2D.OverlapBox(
                    (Vector2)transform.position, new Vector2(1.5f, 2f), 0f, playerLayer);
                if (playerHit != null && !hit.Contains(playerHit))
                {
                    hit.Add(playerHit);
                    IDamageable dmg = playerHit.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, damage * waveformDamage,
                            new Vector2(facingX * 6f, 4f), DamageType.Normal, gameObject);
                }

                yield return null;
            }

            if (trail != null) Destroy(trail, 0.5f);
            isInvincible = false;

            // Notify clone
            _clone?.MirrorWaveform(-facingX);

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        // ── Attack 2: Adaptive Strike ──────────────────────────────────────

        private IEnumerator PerformAdaptiveStrike()
        {
            _isActing      = true;
            _adaptiveTimer = adaptiveCooldown;

            float dist = DistanceToPlayer;

            if (dist >= adaptiveRangeThreshold)
            {
                // Ranged: fire water bolt
                yield return StartCoroutine(FireWaterBolt());
            }
            else
            {
                // Melee: water fist slam
                yield return StartCoroutine(WaterFist());
            }

            // Notify clone
            _clone?.MirrorAdaptive();

            _isActing = false;
        }

        private IEnumerator FireWaterBolt()
        {
            if (animator != null) animator.SetTrigger("RangedAttack");
            yield return new WaitForSeconds(0.25f);

            if (playerTransform != null && waterBoltPrefab != null)
            {
                Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                GameObject bolt = Instantiate(waterBoltPrefab,
                    (Vector2)transform.position + dir * 0.8f, Quaternion.identity);
                SimpleProjectile proj = bolt.GetComponent<SimpleProjectile>();
                if (proj == null) proj = bolt.AddComponent<SimpleProjectile>();
                proj.Initialise(dir, rangedBoltSpeed, damage * 0.85f, playerLayer, gameObject, 5f);
            }

            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator WaterFist()
        {
            if (animator != null) animator.SetTrigger("MeleeAttack");
            yield return new WaitForSeconds(0.2f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(1.8f, 1.5f), 0f, playerLayer);

            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(facingX * 5f, 3f), DamageType.Normal, gameObject);
            }

            yield return new WaitForSeconds(0.35f);
        }

        // ── Phase 2: Water Clone ───────────────────────────────────────────

        private void SpawnWaterClone()
        {
            if (waterClonePrefab == null) return;

            Vector3 clonePos = transform.position + Vector3.right * -cloneOffset;
            GameObject cloneGO = Instantiate(waterClonePrefab, clonePos, Quaternion.identity);
            _clone = cloneGO.GetComponent<WaterCloneController>();
            if (_clone == null) _clone = cloneGO.AddComponent<WaterCloneController>();
            _clone.Initialise(this, damage * 0.6f, playerLayer);
        }

        // ── Phase 2: Arena Flooding ────────────────────────────────────────

        private IEnumerator FloodArenaBottom()
        {
            if (floodZonePrefab == null) yield break;

            // Find ground Y position
            float groundY = _ai.ArenaCenter.y - 4f; // approximate bottom of arena

            Vector3 floodPos = new Vector3(_ai.ArenaCenter.x, groundY + floodZoneHeight * 0.5f, 0f);
            GameObject flood = Instantiate(floodZonePrefab, floodPos, Quaternion.identity);
            flood.transform.localScale = new Vector3(_ai.ArenaWidth, floodZoneHeight, 1f);

            // Add damage zone to flood
            BurningGroundZone zone = flood.GetComponent<BurningGroundZone>();
            if (zone == null) zone = flood.AddComponent<BurningGroundZone>();
            zone.Initialise(floodDuration, 12f, playerLayer, gameObject);

            Destroy(flood, floodDuration + 0.1f);
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, adaptiveRangeThreshold);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Water clone that mirrors The Flood's attacks from the opposite side of the arena.
    /// </summary>
    public class WaterCloneController : MonoBehaviour
    {
        private Boss4TheFlood _boss;
        private float _damage;
        private LayerMask _playerLayer;
        private SpriteRenderer _sr;

        public void Initialise(Boss4TheFlood boss, float damage, LayerMask playerLayer)
        {
            _boss        = boss;
            _damage      = damage;
            _playerLayer = playerLayer;
            _sr          = GetComponent<SpriteRenderer>();

            if (_sr != null)
            {
                Color c = new Color(0.3f, 0.7f, 1f, 0.55f);
                _sr.color = c;
            }
        }

        public void MirrorWaveform(float direction)
        {
            StartCoroutine(CloneDash(direction));
        }

        public void MirrorAdaptive()
        {
            StartCoroutine(CloneMeleeSwing());
        }

        private IEnumerator CloneDash(float dir)
        {
            float elapsed = 0f;
            float dashTime = 0.6f;
            HashSet<Collider2D> hit = new HashSet<Collider2D>();

            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                transform.position += Vector3.right * dir * 15f * Time.deltaTime;

                Collider2D playerHit = Physics2D.OverlapBox(
                    (Vector2)transform.position, new Vector2(1.2f, 1.5f), 0f, _playerLayer);
                if (playerHit != null && !hit.Contains(playerHit))
                {
                    hit.Add(playerHit);
                    IDamageable dmg = playerHit.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, _damage,
                            new Vector2(dir * 5f, 3f), DamageType.Normal, _boss != null ? _boss.gameObject : gameObject);
                }
                yield return null;
            }
        }

        private IEnumerator CloneMeleeSwing()
        {
            yield return new WaitForSeconds(0.2f);

            Collider2D playerHit = Physics2D.OverlapBox(
                (Vector2)transform.position, new Vector2(2f, 1.5f), 0f, _playerLayer);
            if (playerHit != null)
            {
                IDamageable dmg = playerHit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, _damage, new Vector2(0f, 3f),
                        DamageType.Normal, _boss != null ? _boss.gameObject : gameObject);
            }
        }

        private void OnDestroy()
        {
            // If the clone dies, just fade out
        }
    }

    /// <summary>
    /// Simple directional projectile used by The Flood's water bolt.
    /// </summary>
    public class SimpleProjectile : MonoBehaviour
    {
        private Vector2 _dir;
        private float _speed;
        private float _damage;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private bool _hit;

        public void Initialise(Vector2 dir, float speed, float damage,
            LayerMask playerLayer, GameObject owner, float lifetime)
        {
            _dir         = dir;
            _speed       = speed;
            _damage      = damage;
            _playerLayer = playerLayer;
            _owner       = owner;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (_hit) return;
            transform.position += (Vector3)(_dir * _speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hit) return;
            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) == 0) return;

            _hit = true;
            IDamageable dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
                DamageSystem.Instance?.DealDamage(dmg, _damage, _dir * 3f, DamageType.Normal, _owner);

            Destroy(gameObject);
        }
    }
}
