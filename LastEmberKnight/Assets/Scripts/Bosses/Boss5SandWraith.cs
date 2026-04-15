using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 5 – Sand Wraith (Zone 5, Level 29).
    /// A desert predator who attacks from beneath the ground.
    /// 380 HP / 24 damage / 30x38 size.
    /// Phase 2 "Epicenter": 2s channel with 5 expanding damage rings.
    /// Burrowstrike leaves sand trail that slows the player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss5SandWraith : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Sand Wraith – Stats")]
        [SerializeField] private float moveSpeed = 3.2f;

        [Header("Burrowstrike")]
        [SerializeField] private float burrowSpeed = 6f;
        [SerializeField] private float burrowCooldown = 5f;
        [SerializeField] private float burrowStunDuration = 0.6f;
        [SerializeField] private GameObject sandTrailPrefab;
        [SerializeField] private float sandTrailDuration = 4f;
        [SerializeField] private float sandSlowAmount = 0.35f;

        [Header("Sand Storm")]
        [SerializeField] private float stormDuration = 4f;
        [SerializeField] private float stormTickDamage = 4f;
        [SerializeField] private float stormTickInterval = 0.5f;
        [SerializeField] private float stormRadius = 3f;
        [SerializeField] private float stormCooldown = 8f;
        [SerializeField] private GameObject stormParticlesPrefab;

        [Header("Epicenter (Phase 2)")]
        [SerializeField] private int epicenterRings = 5;
        [SerializeField] private float epicenterChannelTime = 2f;
        [SerializeField] private float epicenterRingInterval = 0.3f;
        [SerializeField] private float epicenterRingStartRadius = 1.5f;
        [SerializeField] private float epicenterRingRadiusStep = 1.8f;
        [SerializeField] private float epicenterCooldown = 12f;
        [SerializeField] private GameObject epicenterRingPrefab;

        [Header("Melee")]
        [SerializeField] private float meleeRange = 1.6f;
        [SerializeField] private float meleeCooldown = 1.5f;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask groundLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.80f, 0.65f, 0.30f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.90f, 0.50f, 0.10f, 1f);
        [SerializeField] private Color burrowColor = new Color(0.60f, 0.45f, 0.20f, 0.5f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _burrowTimer;
        private float _stormTimer;
        private float _epicenterTimer;
        private float _meleeTimer;
        private bool _isActing;
        private bool _isBurrowed;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Sand Wraith";

        protected override void OnPhaseTransition()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            _epicenterTimer = 2f; // Use Epicenter soon after phase 2 starts
            Debug.Log("[SandWraith] Phase 2 – Epicenter unlocked!");
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Sand Wraith";
            bossIndex   = 5;
            maxHP       = 380f;
            damage      = 24f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(30f / 32f, 38f / 32f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing || _isBurrowed) return;

            _burrowTimer    -= Time.deltaTime;
            _stormTimer     -= Time.deltaTime;
            _meleeTimer     -= Time.deltaTime;
            _epicenterTimer -= Time.deltaTime;

            float dist = DistanceToPlayer;

            // Phase 2 Epicenter takes priority
            if (phase == 2 && _epicenterTimer <= 0f)
            {
                StartCoroutine(PerformEpicenter());
                return;
            }

            if (_burrowTimer <= 0f)
            {
                StartCoroutine(PerformBurrowstrike());
                return;
            }

            if (_stormTimer <= 0f && dist <= stormRadius * 1.5f)
            {
                StartCoroutine(PerformSandStorm());
                return;
            }

            if (_meleeTimer <= 0f && dist <= meleeRange)
            {
                StartCoroutine(PerformMelee());
                return;
            }

            if (dist > meleeRange)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Attack 1: Burrowstrike ─────────────────────────────────────────

        private IEnumerator PerformBurrowstrike()
        {
            _isActing    = true;
            _isBurrowed  = true;
            _burrowTimer = burrowCooldown;

            if (animator != null) animator.SetTrigger("Burrow");

            // Become temporarily invulnerable while underground
            isInvincible = true;

            if (spriteRenderer != null)
                spriteRenderer.color = burrowColor;

            // Disable collider while burrowed
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // Briefly disappear below ground
            Vector3 originalPos = transform.position;
            Rigidbody2D bossRb = GetComponent<Rigidbody2D>();
            if (bossRb != null) bossRb.gravityScale = 0f;

            // Move underground (downward)
            Vector3 underPos = transform.position + Vector3.down * 3f;
            float elapsed = 0f;
            float sinkTime = 0.3f;
            while (elapsed < sinkTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(originalPos, underPos, elapsed / sinkTime);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            // Travel toward player underground
            if (playerTransform != null)
            {
                Vector3 targetUnder = new Vector3(playerTransform.position.x,
                    underPos.y, 0f);
                float travelTime = Vector3.Distance(transform.position, targetUnder) / burrowSpeed;

                // Spawn sand trail above ground as boss travels
                SpawnSandTrail(transform.position, targetUnder);

                elapsed = 0f;
                Vector3 startUnder = transform.position;
                while (elapsed < travelTime)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(startUnder, targetUnder, elapsed / travelTime);
                    yield return null;
                }
            }

            // Erupt upward
            if (bossRb != null) bossRb.gravityScale = 1f;
            if (col != null) col.enabled = true;

            Vector3 eruptPos = new Vector3(transform.position.x, originalPos.y, 0f);
            elapsed = 0f;
            float eruptTime = 0.25f;
            Vector3 belowPos = transform.position;
            while (elapsed < eruptTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(belowPos, eruptPos, elapsed / eruptTime);
                yield return null;
            }

            // Damage + stun player at surface
            if (animator != null) animator.SetTrigger("Erupt");

            Collider2D playerHit = Physics2D.OverlapCircle(
                (Vector2)transform.position, 1.2f, playerLayer);
            if (playerHit != null)
            {
                IDamageable dmg = playerHit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage * 1.3f,
                        new Vector2(0f, 6f), DamageType.Normal, gameObject);

                // Stun
                PlayerMovementDebuff stun = playerHit.GetComponent<PlayerMovementDebuff>();
                if (stun == null) stun = playerHit.gameObject.AddComponent<PlayerMovementDebuff>();
                stun.ApplyStun(burrowStunDuration);
            }

            isInvincible = false;
            _isBurrowed  = false;

            if (spriteRenderer != null)
                spriteRenderer.color = (phase == 2) ? phase2Color : phase1Color;

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        private void SpawnSandTrail(Vector3 from, Vector3 to)
        {
            if (sandTrailPrefab == null) return;

            Vector3 midpoint  = (from + to) * 0.5f;
            Vector3 aboveMid  = new Vector3(midpoint.x, midpoint.y + 2.5f, 0f);
            float   trailLen  = Vector3.Distance(from, to);

            GameObject trail = Instantiate(sandTrailPrefab, aboveMid, Quaternion.identity);
            trail.transform.localScale = new Vector3(trailLen, 0.5f, 1f);

            // Add slow zone to trail
            SandSlowZone slow = trail.GetComponent<SandSlowZone>();
            if (slow == null) slow = trail.AddComponent<SandSlowZone>();
            slow.Initialise(sandTrailDuration, sandSlowAmount, playerLayer);

            Destroy(trail, sandTrailDuration);
        }

        // ── Attack 2: Sand Storm ───────────────────────────────────────────

        private IEnumerator PerformSandStorm()
        {
            _isActing   = true;
            _stormTimer = stormCooldown;

            if (animator != null) animator.SetTrigger("Storm");

            GameObject stormFX = null;
            if (stormParticlesPrefab != null)
            {
                stormFX = Instantiate(stormParticlesPrefab, transform.position, Quaternion.identity);
                stormFX.transform.SetParent(transform);
            }

            // Boss becomes invisible
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 0.15f;
                spriteRenderer.color = c;
            }

            float elapsed   = 0f;
            float tickTimer = 0f;

            while (elapsed < stormDuration)
            {
                elapsed   += Time.deltaTime;
                tickTimer += Time.deltaTime;

                if (tickTimer >= stormTickInterval)
                {
                    tickTimer = 0f;

                    // Tick damage to nearby player
                    Collider2D playerHit = Physics2D.OverlapCircle(
                        (Vector2)transform.position, stormRadius, playerLayer);
                    if (playerHit != null)
                    {
                        IDamageable dmg = playerHit.GetComponent<IDamageable>();
                        if (dmg != null)
                            DamageSystem.Instance?.DealDamage(dmg, stormTickDamage,
                                Vector2.zero, DamageType.Normal, gameObject);
                    }
                }

                yield return null;
            }

            // Reappear
            if (stormFX != null) Destroy(stormFX);

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }

            yield return new WaitForSeconds(0.3f);
            _isActing = false;
        }

        // ── Phase 2: Epicenter ─────────────────────────────────────────────

        private IEnumerator PerformEpicenter()
        {
            _isActing       = true;
            _epicenterTimer = epicenterCooldown;

            if (animator != null) animator.SetTrigger("Channel");

            // Channel for 2 seconds with visible pulses
            yield return StartCoroutine(_ai.TelegraphAttack(epicenterChannelTime, transform.position));

            if (animator != null) animator.SetTrigger("Epicenter");

            // Fire expanding rings
            for (int i = 0; i < epicenterRings; i++)
            {
                float ringRadius = epicenterRingStartRadius + i * epicenterRingRadiusStep;
                SpawnDamageRing(ringRadius, i * 0.05f);
                yield return new WaitForSeconds(epicenterRingInterval);
            }

            yield return new WaitForSeconds(0.6f);
            _isActing = false;
        }

        private void SpawnDamageRing(float radius, float delay)
        {
            StartCoroutine(DamageRingCoroutine(radius, delay));
        }

        private IEnumerator DamageRingCoroutine(float radius, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Spawn visual ring
            if (epicenterRingPrefab != null)
            {
                GameObject ring = Instantiate(epicenterRingPrefab,
                    transform.position, Quaternion.identity);
                ring.transform.localScale = Vector3.one * radius * 2f;
                Destroy(ring, 0.5f);
            }

            // Damage check in ring band (annulus)
            Collider2D[] all = Physics2D.OverlapCircleAll(
                (Vector2)transform.position, radius + 0.5f, playerLayer);
            foreach (Collider2D c in all)
            {
                float dist = Vector2.Distance(transform.position, c.transform.position);
                if (dist >= radius - 0.5f) // within the ring band
                {
                    IDamageable dmg = c.GetComponent<IDamageable>();
                    if (dmg != null)
                    {
                        Vector2 kb = ((Vector2)c.transform.position - (Vector2)transform.position).normalized * 5f;
                        DamageSystem.Instance?.DealDamage(dmg, damage * 1.1f,
                            kb, DamageType.Normal, gameObject);
                    }
                }
            }
        }

        // ── Basic Melee ────────────────────────────────────────────────────

        private IEnumerator PerformMelee()
        {
            _isActing   = true;
            _meleeTimer = meleeCooldown;

            if (animator != null) animator.SetTrigger("Attack");
            yield return new WaitForSeconds(0.2f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX * 0.8f;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(1.6f, 1.3f), 0f, playerLayer);
            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(facingX * 4f, 2f), DamageType.Normal, gameObject);
            }

            yield return new WaitForSeconds(0.4f);
            _isActing = false;
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, stormRadius);

            for (int i = 0; i < epicenterRings; i++)
            {
                float r = epicenterRingStartRadius + i * epicenterRingRadiusStep;
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, r);
            }
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Sand trail zone that slows the player while standing in it.
    /// </summary>
    public class SandSlowZone : MonoBehaviour
    {
        private float _duration;
        private float _slowAmount;
        private LayerMask _playerLayer;
        private Collider2D _playerInside;

        public void Initialise(float duration, float slowAmount, LayerMask playerLayer)
        {
            _duration    = duration;
            _slowAmount  = slowAmount;
            _playerLayer = playerLayer;

            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            Destroy(gameObject, duration);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) == 0) return;
            _playerInside = other;

            PlayerMovementDebuff debuff = other.GetComponent<PlayerMovementDebuff>();
            if (debuff == null) debuff = other.gameObject.AddComponent<PlayerMovementDebuff>();
            debuff.ApplySlowDebuff(_slowAmount, "SandTrail_" + GetInstanceID());
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != _playerInside) return;
            _playerInside = null;

            PlayerMovementDebuff debuff = other.GetComponent<PlayerMovementDebuff>();
            debuff?.RemoveSlowDebuff("SandTrail_" + GetInstanceID());
        }

        private void OnDestroy()
        {
            if (_playerInside != null)
            {
                PlayerMovementDebuff debuff = _playerInside.GetComponent<PlayerMovementDebuff>();
                debuff?.RemoveSlowDebuff("SandTrail_" + GetInstanceID());
            }
        }
    }
}
