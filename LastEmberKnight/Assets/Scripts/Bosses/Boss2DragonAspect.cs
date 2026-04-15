using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 2 – Dragon Aspect (Zone 2, Level 14).
    /// A warrior who channels the power of an ancient dragon.
    /// 500 HP / 30 damage / 42x38 size.
    /// Phase 2: transforms into a larger dragon form (scale *1.4), gains pseudo-flight
    /// and rains 3 fireballs that leave 2s burning ground zones.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss2DragonAspect : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Dragon Aspect – Stats")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Header("Dragon Breath")]
        [SerializeField] private float breathSweepAngle = 120f;
        [SerializeField] private float breathRange = 5f;
        [SerializeField] private float breathDamagePerSecond = 20f;
        [SerializeField] private float breathDuration = 1.8f;
        [SerializeField] private float breathCooldown = 7f;
        [SerializeField] private GameObject breathParticlesPrefab;

        [Header("Dragon Tail")]
        [SerializeField] private float tailRange = 1.5f;
        [SerializeField] private float tailCooldown = 3f;
        [SerializeField] private float tailKnockbackForce = 9f;

        [Header("Phase 2 – Dragon Form")]
        [SerializeField] private float phase2ScaleMultiplier = 1.4f;
        [SerializeField] private float highJumpForce = 18f;
        [SerializeField] private int fireballCount = 3;
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private float fireballRainCooldown = 8f;
        [SerializeField] private float fireballSpreadX = 3f;

        [Header("Burning Ground")]
        [SerializeField] private GameObject burningGroundPrefab;
        [SerializeField] private float burningGroundDuration = 2f;
        [SerializeField] private float burningGroundDPS = 10f;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.75f, 0.20f, 0.05f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.95f, 0.40f, 0.02f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _breathTimer;
        private float _tailTimer;
        private float _fireballRainTimer;
        private bool _isActing;
        private bool _isFlying;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Dragon Aspect";

        protected override void OnPhaseTransition()
        {
            // Scale up to dragon form
            transform.localScale *= phase2ScaleMultiplier;

            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[DragonAspect] Phase 2 – Dragon Form activated!");

            // Shake camera effect via BossPhaseManager
            BossPhaseManager phaseManager = GetComponentInChildren<BossPhaseManager>();
            phaseManager?.TriggerPhaseTransition(this);
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Dragon Aspect";
            bossIndex   = 2;
            maxHP       = 500f;
            damage      = 30f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(42f / 32f, 38f / 32f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _breathTimer       -= Time.deltaTime;
            _tailTimer         -= Time.deltaTime;
            _fireballRainTimer -= Time.deltaTime;

            float dist = DistanceToPlayer;

            // Phase 2: Fireball rain
            if (phase == 2 && _fireballRainTimer <= 0f)
            {
                StartCoroutine(PerformFireballRain());
                return;
            }

            // Dragon Tail – behind boss
            if (_tailTimer <= 0f && IsPlayerBehindBoss() && dist <= tailRange)
            {
                StartCoroutine(PerformDragonTail());
                return;
            }

            // Dragon Breath – in front
            if (_breathTimer <= 0f && dist <= breathRange)
            {
                StartCoroutine(PerformDragonBreath());
                return;
            }

            // Move toward player or perform phase 2 jumps
            if (phase == 2 && _isFlying)
                return;

            if (dist > 1.5f)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Attack 1: Dragon Breath ────────────────────────────────────────

        /// <summary>
        /// Fire beam that sweeps 120 degrees. The player must duck or jump to avoid.
        /// </summary>
        private IEnumerator PerformDragonBreath()
        {
            _isActing    = true;
            _breathTimer = breathCooldown;

            if (animator != null) animator.SetTrigger("Breath");

            // Spawn breath particle system
            GameObject breathFX = null;
            if (breathParticlesPrefab != null)
            {
                breathFX = Instantiate(breathParticlesPrefab, transform.position, Quaternion.identity);
                breathFX.transform.SetParent(transform);
            }

            // Sweep over breathDuration
            float elapsed = 0f;
            float startAngle = -(breathSweepAngle * 0.5f);
            float endAngle   =  (breathSweepAngle * 0.5f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            if (facingX < 0f)
            {
                startAngle += 180f;
                endAngle   += 180f;
            }

            while (elapsed < breathDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / breathDuration;
                float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                Vector2 dir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                                          Mathf.Sin(currentAngle * Mathf.Deg2Rad));

                // Rotate breath FX
                if (breathFX != null)
                    breathFX.transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

                // Raycast for player hit in breath direction
                RaycastHit2D rayHit = Physics2D.Raycast(
                    (Vector2)transform.position, dir, breathRange, playerLayer);
                if (rayHit.collider != null)
                {
                    IDamageable dmg = rayHit.collider.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, breathDamagePerSecond * Time.deltaTime,
                            Vector2.zero, DamageType.Normal, gameObject);
                }

                yield return null;
            }

            if (breathFX != null) Destroy(breathFX);
            _isActing = false;
        }

        // ── Attack 2: Dragon Tail ──────────────────────────────────────────

        private bool IsPlayerBehindBoss()
        {
            if (playerTransform == null) return false;
            float facingX  = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            float playerDX = playerTransform.position.x - transform.position.x;
            // Player is "behind" if they are on the opposite side of where boss faces
            return Mathf.Sign(playerDX) != Mathf.Sign(facingX);
        }

        private IEnumerator PerformDragonTail()
        {
            _isActing  = true;
            _tailTimer = tailCooldown;

            if (animator != null) animator.SetTrigger("Tail");

            yield return new WaitForSeconds(0.15f);

            float facingX   = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            float tailDirX  = -facingX; // Tail hits behind
            Vector2 origin  = (Vector2)transform.position + Vector2.right * tailDirX * 0.8f;

            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(tailRange * 2f, 1.5f),
                0f, playerLayer);
            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(tailDirX * tailKnockbackForce, 3f),
                        DamageType.Normal, gameObject);
            }

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        // ── Phase 2: Fireball Rain ─────────────────────────────────────────

        private IEnumerator PerformFireballRain()
        {
            _isActing          = true;
            _fireballRainTimer = fireballRainCooldown;

            if (animator != null) animator.SetTrigger("Jump");

            // High jump for pseudo-flight
            Rigidbody2D bossRb = GetComponent<Rigidbody2D>();
            if (bossRb != null)
            {
                bossRb.velocity = new Vector2(bossRb.velocity.x, highJumpForce);
                _isFlying = true;
            }

            yield return new WaitForSeconds(0.6f);

            // Rain 3 fireballs spread around player position
            Vector2 targetCenter = playerTransform != null
                ? (Vector2)playerTransform.position
                : (Vector2)transform.position;

            for (int i = 0; i < fireballCount; i++)
            {
                float offsetX = (i - fireballCount / 2) * fireballSpreadX;
                Vector3 spawnPos = new Vector3(targetCenter.x + offsetX,
                    transform.position.y + 2f, 0f);

                SpawnFireball(spawnPos, targetCenter + new Vector2(offsetX, -4f));
                yield return new WaitForSeconds(0.15f);
            }

            yield return new WaitForSeconds(0.8f);
            _isFlying = false;
            _isActing = false;
        }

        private void SpawnFireball(Vector3 origin, Vector2 targetPos)
        {
            if (fireballPrefab == null) return;

            GameObject fb = Instantiate(fireballPrefab, origin, Quaternion.identity);
            BossFireball fireballScript = fb.GetComponent<BossFireball>();
            if (fireballScript == null)
                fireballScript = fb.AddComponent<BossFireball>();

            fireballScript.Initialise(targetPos, damage * 0.8f, burningGroundPrefab,
                burningGroundDuration, burningGroundDPS, playerLayer, gameObject);
        }
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Fireball projectile that falls toward a target, then spawns a burning ground zone.
    /// </summary>
    public class BossFireball : MonoBehaviour
    {
        private Vector2 _target;
        private float _damage;
        private GameObject _burningGroundPrefab;
        private float _burningDuration;
        private float _burningDPS;
        private LayerMask _playerLayer;
        private GameObject _owner;

        private float _speed = 10f;
        private bool _landed;

        public void Initialise(Vector2 target, float damage, GameObject burningGroundPrefab,
            float burningDuration, float burningDPS, LayerMask playerLayer, GameObject owner)
        {
            _target              = target;
            _damage              = damage;
            _burningGroundPrefab = burningGroundPrefab;
            _burningDuration     = burningDuration;
            _burningDPS          = burningDPS;
            _playerLayer         = playerLayer;
            _owner               = owner;
        }

        private void Update()
        {
            if (_landed) return;
            transform.position = Vector3.MoveTowards(transform.position,
                new Vector3(_target.x, _target.y, 0f), _speed * Time.deltaTime);

            if (Vector2.Distance(transform.position, _target) < 0.3f)
                Land();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_landed) return;
            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) != 0)
            {
                IDamageable dmg = other.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, _damage, Vector2.up * 3f,
                        DamageType.Normal, _owner);
            }
        }

        private void Land()
        {
            _landed = true;

            if (_burningGroundPrefab != null)
            {
                GameObject zone = Instantiate(_burningGroundPrefab,
                    new Vector3(_target.x, _target.y, 0f), Quaternion.identity);
                BurningGroundZone bzScript = zone.GetComponent<BurningGroundZone>();
                if (bzScript == null) bzScript = zone.AddComponent<BurningGroundZone>();
                bzScript.Initialise(_burningDuration, _burningDPS, _playerLayer, _owner);
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Burning ground damage zone placed by Dragon Aspect's fireballs.
    /// Uses a BoxCollider2D trigger and deals DPS to the player.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BurningGroundZone : MonoBehaviour
    {
        private float _duration;
        private float _dps;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private bool _playerInside;
        private Collider2D _playerCollider;

        public void Initialise(float duration, float dps, LayerMask playerLayer, GameObject owner)
        {
            _duration    = duration;
            _dps         = dps;
            _playerLayer = playerLayer;
            _owner       = owner;

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null) col.isTrigger = true;

            Destroy(gameObject, duration);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) != 0)
            {
                _playerInside   = true;
                _playerCollider = other;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == _playerCollider)
            {
                _playerInside   = false;
                _playerCollider = null;
            }
        }

        private void Update()
        {
            if (!_playerInside || _playerCollider == null) return;

            IDamageable dmg = _playerCollider.GetComponent<IDamageable>();
            if (dmg != null)
                DamageSystem.Instance?.DealDamage(dmg, _dps * Time.deltaTime,
                    Vector2.zero, DamageType.Normal, _owner);
        }
    }
}
