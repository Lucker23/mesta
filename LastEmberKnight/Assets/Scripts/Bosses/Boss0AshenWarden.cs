using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 0 – Ashen Warden (Zone 0, Level 4).
    /// A brutish warrior who forces enemies to fight him up close, then unleashes
    /// devastating cleave and spin attacks.
    /// 300 HP / 25 damage / 34x42 size.
    /// Visual: dark red/brown, spiky silhouette.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss0AshenWarden : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Ashen Warden – Stats")]
        [SerializeField] private float moveSpeed = 3f;

        [Header("Battle Roar")]
        [SerializeField] private float roarRadius = 5f;
        [SerializeField] private float roarForceDuration = 1f;
        [SerializeField] private float cleaveArcHalfAngle = 70f;
        [SerializeField] private float cleaveRange = 2.5f;
        [SerializeField] private float roarCooldown = 6f;
        [SerializeField] private GameObject roarShockwavePrefab;

        [Header("Counter Helix")]
        [SerializeField] private float helixRadius = 2.2f;
        [SerializeField] private float helixDamageMultiplier = 0.8f;
        [SerializeField] private float phase1SpinChance = 0.30f;
        [SerializeField] private float phase2SpinChance = 0.50f;
        [SerializeField] private GameObject helixParticlesPrefab;

        [Header("Melee")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackCooldown = 1.6f;
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.35f, 0.10f, 0.08f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.55f, 0.08f, 0.04f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _attackTimer;
        private float _roarTimer;
        private bool _isActing;
        private bool _playerPulled;
        private float _pullTimer;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Ashen Warden";

        protected override void OnPhaseTransition()
        {
            // Double the roar radius in phase 2
            roarRadius *= 2f;

            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[AshenWarden] Phase 2 activated – Battle Roar range doubled!");
        }

        protected override void ExecuteAttack()
        {
            // Primary logic driven by Update coroutines
        }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName  = "Ashen Warden";
            bossIndex = 0;
            maxHP     = 300f;
            damage    = 25f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = phase1Color;
                // Scale to 34x42 pixels (in world units, 1 unit = assumed pixel density)
                transform.localScale = new Vector3(34f / 32f, 42f / 32f, 1f);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _attackTimer -= Time.deltaTime;
            _roarTimer   -= Time.deltaTime;

            // Tick pull state
            if (_playerPulled)
            {
                _pullTimer -= Time.deltaTime;
                if (_pullTimer <= 0f)
                {
                    _playerPulled = false;
                    StartCoroutine(PerformCleave());
                }
                return;
            }

            // Priority: Roar if off cooldown, otherwise melee
            if (_roarTimer <= 0f)
            {
                StartCoroutine(PerformBattleRoar());
                return;
            }

            float dist = DistanceToPlayer;
            if (dist <= attackRange && _attackTimer <= 0f)
            {
                StartCoroutine(PerformMeleeSwing());
            }
            else if (dist > attackRange)
            {
                _ai.MoveTowardPlayer(moveSpeed);
            }
        }

        // ── Attack 1: Battle Roar ──────────────────────────────────────────

        /// <summary>
        /// Taunts in radius, forces player toward boss for 1 second, then cleave.
        /// Phase 2 roar range is doubled.
        /// </summary>
        private IEnumerator PerformBattleRoar()
        {
            _isActing = true;
            _roarTimer = roarCooldown;

            if (animator != null) animator.SetTrigger("Roar");

            // Visual shockwave ring
            if (roarShockwavePrefab != null)
            {
                GameObject sw = Instantiate(roarShockwavePrefab, transform.position, Quaternion.identity);
                sw.transform.localScale = Vector3.one * roarRadius * 2f;
                Destroy(sw, 0.8f);
            }

            // Check player in radius
            if (playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= roarRadius)
                {
                    // Pull player toward boss
                    _playerPulled = true;
                    _pullTimer    = roarForceDuration;

                    StartCoroutine(PullPlayerTowardBoss());
                }
            }

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        private IEnumerator PullPlayerTowardBoss()
        {
            float elapsed = 0f;
            while (elapsed < roarForceDuration && playerTransform != null)
            {
                elapsed += Time.deltaTime;

                // Apply impulse toward boss each frame
                Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 dir = ((Vector2)transform.position - (Vector2)playerTransform.position).normalized;
                    playerRb.AddForce(dir * 6f, ForceMode2D.Force);
                }

                yield return null;
            }
        }

        // ── Cleave Attack ──────────────────────────────────────────────────

        private IEnumerator PerformCleave()
        {
            _isActing = true;

            if (animator != null) animator.SetTrigger("Cleave");

            // Telegraph
            yield return StartCoroutine(_ai.TelegraphAttack(0.4f, transform.position));

            // Wide arc damage check
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, cleaveRange, playerLayer);
            foreach (Collider2D hit in hits)
            {
                // Angle check
                Vector2 dir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
                Vector2 facing = new Vector2(facingX, 0f);
                float angle = Vector2.Angle(facing, dir);

                if (angle <= cleaveArcHalfAngle)
                {
                    IDamageable dmg = hit.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, damage, Vector2.right * facingX * 5f,
                            DamageType.Normal, gameObject);
                }
            }

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        // ── Attack 2: Counter Helix ────────────────────────────────────────

        protected override void OnHitReaction()
        {
            float spinChance = (phase == 2) ? phase2SpinChance : phase1SpinChance;
            if (Random.value < spinChance && !_isActing)
                StartCoroutine(PerformCounterHelix());
        }

        private IEnumerator PerformCounterHelix()
        {
            _isActing = true;

            if (animator != null) animator.SetTrigger("Spin");

            if (helixParticlesPrefab != null)
            {
                GameObject fx = Instantiate(helixParticlesPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 1.2f);
            }

            // Spin AOE – hits everything in radius
            yield return new WaitForSeconds(0.15f); // brief startup

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, helixRadius, playerLayer);
            foreach (Collider2D hit in hits)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Vector2 kb = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized * 4f;
                    DamageSystem.Instance?.DealDamage(dmg, damage * helixDamageMultiplier,
                        kb, DamageType.Normal, gameObject);
                }
            }

            yield return new WaitForSeconds(0.6f);
            _isActing = false;
        }

        // ── Basic Melee ────────────────────────────────────────────────────

        private IEnumerator PerformMeleeSwing()
        {
            _isActing = true;
            _attackTimer = attackCooldown;

            if (animator != null) animator.SetTrigger("Attack");

            yield return new WaitForSeconds(0.2f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX * 1f;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(2f, 1.5f), 0f, playerLayer);
            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage, new Vector2(facingX * 4f, 2f),
                        DamageType.Normal, gameObject);
            }

            yield return new WaitForSeconds(0.4f);
            _isActing = false;
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, roarRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, helixRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, cleaveRange);
        }
#endif
    }
}
