using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 8 – Storm Drake (Zone 8, Level 44).
    /// A lightning-wreathed entity of pure kinetic energy.
    /// 600 HP / 30 damage / 44x38 size.
    /// Phase 2: Ball Lightning leaves 3s electric trail, 3 remnants chain,
    /// attacks MUCH faster.
    /// Visual: electric blue/white, speed lines.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss8StormDrake : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Storm Drake – Stats")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float phase2MoveSpeed = 7f;

        [Header("Ball Lightning")]
        [SerializeField] private float ballLightningHPCost = 0.05f;   // 5% own HP
        [SerializeField] private float ballLightningCooldown = 4f;
        [SerializeField] private float ballLightningPhase2Cooldown = 2f;
        [SerializeField] private float ballLightningDamagePerUnit = 4f;
        [SerializeField] private bool phase2LeavesTrail = false;
        [SerializeField] private float trailDuration = 3f;
        [SerializeField] private GameObject lightningTrailPrefab;
        [SerializeField] private GameObject ballLightningVFXPrefab;

        [Header("Overload")]
        [SerializeField] private float overloadLightningDamage = 0.6f; // multiplier
        [SerializeField] private float overloadChainRadius = 3f;
        [SerializeField] private GameObject chainLightningPrefab;

        [Header("Static Remnant")]
        [SerializeField] private float remnantDelay = 2f;
        [SerializeField] private float remnantExplosionRadius = 2.5f;
        [SerializeField] private GameObject staticRemnantPrefab;
        [SerializeField] private float remnantCooldown = 5f;
        [SerializeField] private float remnantPhase2Cooldown = 2f;
        [SerializeField] private int phase2MaxRemnants = 3;

        [Header("Melee")]
        [SerializeField] private float meleeRange = 1.8f;
        [SerializeField] private float meleeCooldown = 1.2f;
        [SerializeField] private float phase2MeleeCooldown = 0.7f;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.50f, 0.80f, 1.00f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.85f, 0.95f, 1.00f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _ballLightningTimer;
        private float _remnantTimer;
        private float _meleeTimer;
        private bool _isActing;
        private bool _overloadReady;  // true after using an ability
        private int _activeRemnants;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Storm Drake";

        protected override void OnPhaseTransition()
        {
            phase2LeavesTrail       = true;
            moveSpeed               = phase2MoveSpeed;
            ballLightningCooldown   = ballLightningPhase2Cooldown;
            remnantCooldown         = remnantPhase2Cooldown;
            meleeCooldown           = phase2MeleeCooldown;

            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[StormDrake] Phase 2 – MUCH faster, trail active, remnants chain!");
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Storm Drake";
            bossIndex   = 8;
            maxHP       = 600f;
            damage      = 30f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(44f / 32f, 38f / 32f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _ballLightningTimer -= Time.deltaTime;
            _remnantTimer       -= Time.deltaTime;
            _meleeTimer         -= Time.deltaTime;

            float dist = DistanceToPlayer;

            // Ball Lightning (teleport dash) – highest priority
            if (_ballLightningTimer <= 0f && dist > 2f)
            {
                StartCoroutine(PerformBallLightning());
                return;
            }

            // Static Remnant – at current position before moving
            if (_remnantTimer <= 0f)
            {
                bool canRemnant = phase == 1
                    || (phase == 2 && _activeRemnants < phase2MaxRemnants);
                if (canRemnant)
                {
                    StartCoroutine(DropStaticRemnant());
                    return;
                }
            }

            // Melee with overload check
            if (_meleeTimer <= 0f && dist <= meleeRange)
            {
                StartCoroutine(PerformMelee());
                return;
            }

            if (dist > meleeRange)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Attack 1: Ball Lightning ───────────────────────────────────────

        /// <summary>
        /// Teleport-dash to a point, damaging the entire path.
        /// Costs 5% of own HP.
        /// Phase 2: leaves a 3s electric trail.
        /// </summary>
        private IEnumerator PerformBallLightning()
        {
            _isActing         = true;
            _ballLightningTimer = ballLightningCooldown;

            if (animator != null) animator.SetTrigger("BallLightning");

            // Pay HP cost
            float hpCost = maxHP * ballLightningHPCost;
            currentHP = Mathf.Max(1f, currentHP - hpCost);

            // Target: player position
            Vector3 startPos = transform.position;
            Vector3 targetPos = playerTransform != null
                ? playerTransform.position + Vector3.right * (Random.value > 0.5f ? 1.5f : -1.5f)
                : transform.position + Vector3.right * 4f;

            if (ballLightningVFXPrefab != null)
            {
                GameObject vfx = Instantiate(ballLightningVFXPrefab, startPos, Quaternion.identity);
                Destroy(vfx, 1f);
            }

            // Collect all player colliders along path
            float dashTime = 0.15f;
            float elapsed  = 0f;
            HashSet<Collider2D> hit = new HashSet<Collider2D>();

            List<Vector3> trailPositions = new List<Vector3>();

            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashTime;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                trailPositions.Add(transform.position);

                float distTraveled = Vector3.Distance(startPos, transform.position);
                float lineDmg      = distTraveled * ballLightningDamagePerUnit;

                Collider2D playerInPath = Physics2D.OverlapCircle(
                    (Vector2)transform.position, 0.8f, playerLayer);
                if (playerInPath != null && !hit.Contains(playerInPath))
                {
                    hit.Add(playerInPath);
                    IDamageable dmg = playerInPath.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, damage + lineDmg,
                            Vector2.zero, DamageType.Normal, gameObject);
                }

                yield return null;
            }

            transform.position = targetPos;

            // Phase 2: leave electric trail
            if (phase2LeavesTrail && lightningTrailPrefab != null)
            {
                SpawnLightningTrail(startPos, targetPos);
            }

            _overloadReady = true; // Next melee chains lightning
            _isActing = false;
        }

        private void SpawnLightningTrail(Vector3 from, Vector3 to)
        {
            Vector3 midpoint = (from + to) * 0.5f;
            float   length   = Vector3.Distance(from, to);
            float   angle    = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

            GameObject trail = Instantiate(lightningTrailPrefab, midpoint,
                Quaternion.Euler(0f, 0f, angle));
            trail.transform.localScale = new Vector3(length, 0.5f, 1f);

            // Damage zone on trail
            SandSlowZone zone = trail.GetComponent<SandSlowZone>();
            if (zone == null) zone = trail.AddComponent<SandSlowZone>();
            zone.Initialise(trailDuration, 0f, playerLayer); // no slow, just damage via BurningGroundZone

            BurningGroundZone burnZone = trail.GetComponent<BurningGroundZone>();
            if (burnZone == null) burnZone = trail.AddComponent<BurningGroundZone>();
            burnZone.Initialise(trailDuration, 8f, playerLayer, gameObject);

            Destroy(trail, trailDuration);
        }

        // ── Overload ───────────────────────────────────────────────────────

        private IEnumerator PerformMelee()
        {
            _isActing  = true;
            _meleeTimer = meleeCooldown;

            if (animator != null) animator.SetTrigger("Melee");
            yield return new WaitForSeconds(0.15f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(1.8f, 1.4f), 0f, playerLayer);

            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(facingX * 4f, 2f), DamageType.Normal, gameObject);

                // Overload: after using an ability, chain lightning on next melee
                if (_overloadReady)
                {
                    _overloadReady = false;
                    StartCoroutine(ChainLightning(hit.transform.position));
                }
            }

            yield return new WaitForSeconds(0.35f);
            _isActing = false;
        }

        private IEnumerator ChainLightning(Vector3 fromPos)
        {
            if (chainLightningPrefab != null)
            {
                GameObject chainFX = Instantiate(chainLightningPrefab, fromPos, Quaternion.identity);
                Destroy(chainFX, 0.5f);
            }

            // AOE lightning in chain radius
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                (Vector2)fromPos, overloadChainRadius, playerLayer);
            foreach (Collider2D c in hits)
            {
                IDamageable dmg = c.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage * overloadLightningDamage,
                        Vector2.zero, DamageType.Normal, gameObject);
            }

            yield return null;
        }

        // ── Attack 3: Static Remnant ───────────────────────────────────────

        private IEnumerator DropStaticRemnant()
        {
            _isActing     = true;
            _remnantTimer = remnantCooldown;
            _activeRemnants++;

            if (staticRemnantPrefab != null)
            {
                Vector3 pos = transform.position;
                GameObject remnant = Instantiate(staticRemnantPrefab, pos, Quaternion.identity);
                StaticRemnantOrb orbScript = remnant.GetComponent<StaticRemnantOrb>();
                if (orbScript == null) orbScript = remnant.AddComponent<StaticRemnantOrb>();

                bool chainEnabled = (phase == 2);
                orbScript.Initialise(remnantDelay, remnantExplosionRadius, damage * 1.2f,
                    playerLayer, gameObject, chainEnabled, overloadChainRadius,
                    () => _activeRemnants--);
            }

            _overloadReady = true;
            yield return new WaitForSeconds(0.3f);
            _isActing = false;
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, overloadChainRadius);
            Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, remnantExplosionRadius);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Static remnant orb that explodes after a delay.
    /// Phase 2: chains to nearby remnants, triggering a cascade.
    /// </summary>
    public class StaticRemnantOrb : MonoBehaviour
    {
        private float _delay;
        private float _explosionRadius;
        private float _damage;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private bool _chainEnabled;
        private float _chainRadius;
        private System.Action _onDestroyed;
        private bool _exploded;

        public void Initialise(float delay, float explosionRadius, float damage,
            LayerMask playerLayer, GameObject owner, bool chainEnabled,
            float chainRadius, System.Action onDestroyed)
        {
            _delay           = delay;
            _explosionRadius = explosionRadius;
            _damage          = damage;
            _playerLayer     = playerLayer;
            _owner           = owner;
            _chainEnabled    = chainEnabled;
            _chainRadius     = chainRadius;
            _onDestroyed     = onDestroyed;

            StartCoroutine(ExplodeAfterDelay());
        }

        private IEnumerator ExplodeAfterDelay()
        {
            float elapsed = 0f;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            while (elapsed < _delay)
            {
                elapsed += Time.deltaTime;
                // Pulse warning
                if (sr != null)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 8f / _delay);
                    Color c = sr.color;
                    c.a = pulse;
                    sr.color = c;
                }
                yield return null;
            }

            Explode();
        }

        public void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                (Vector2)transform.position, _explosionRadius, _playerLayer);
            foreach (Collider2D c in hits)
            {
                IDamageable dmg = c.GetComponent<IDamageable>();
                if (dmg != null)
                {
                    Vector2 kb = ((Vector2)c.transform.position - (Vector2)transform.position).normalized * 4f;
                    DamageSystem.Instance?.DealDamage(dmg, _damage, kb, DamageType.Normal, _owner);
                }
            }

            // Phase 2: chain to nearby remnants
            if (_chainEnabled)
            {
                Collider2D[] nearbyRemnants = Physics2D.OverlapCircleAll(
                    (Vector2)transform.position, _chainRadius);
                foreach (Collider2D rc in nearbyRemnants)
                {
                    StaticRemnantOrb other = rc.GetComponent<StaticRemnantOrb>();
                    if (other != null && other != this)
                        other.Explode();
                }
            }

            _onDestroyed?.Invoke();
            Destroy(gameObject);
        }
    }
}
