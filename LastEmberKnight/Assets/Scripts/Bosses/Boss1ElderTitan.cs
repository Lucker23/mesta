using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 1 – Elder Titan (Zone 1, Level 9).
    /// THE LUCKER23 EASTER EGG BOSS.
    /// Appears at Level 10 via the Lucker23 hack, replacing the normal boss.
    /// 420 HP / 22 damage / 40x44 size.
    /// Visual: teal/cyan palette.
    /// Easter egg credit displayed under HP bar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss1ElderTitan : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Elder Titan – Easter Egg Credit")]
        [SerializeField] private TextMeshProUGUI easterEggSubtitleText;

        [Header("Astral Spirit")]
        [SerializeField] private GameObject astralSpiritPrefab;
        [SerializeField] private float spiritSpeed = 7f;
        [SerializeField] private float spiritRange = 14f;
        [SerializeField] private float spiritCooldown = 5f;
        [SerializeField] private bool phase2SpiritTrail = false;

        [Header("Earth Splitter")]
        [SerializeField] private GameObject crackLinePrefab;
        [SerializeField] private GameObject splitterHitboxPrefab;
        [SerializeField] private float splitterWidth = 1.5f;
        [SerializeField] private float splitterChargeTime = 1.5f;
        [SerializeField] private float splitterCooldown = 8f;
        [SerializeField] private float splitterLength = 18f;

        [Header("Natural Order")]
        [SerializeField] private float naturalOrderRadius = 4f;
        [SerializeField] private float defenseReductionPercent = 0.30f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.10f, 0.72f, 0.72f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.05f, 0.90f, 0.85f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _spiritTimer;
        private float _splitterTimer;
        private bool _isActing;

        // Tracks whether the Natural Order aura is currently applied to player
        private bool _naturalOrderActive;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Elder Titan";

        protected override void OnPhaseTransition()
        {
            phase2SpiritTrail = true;
            splitterWidth *= 2f;

            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[ElderTitan] Phase 2 – Spirit leaves a trail, Splitter is wider!");
        }

        protected override void ExecuteAttack()
        {
            // Driven by Update coroutines
        }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Elder Titan";
            bossIndex   = 1;
            maxHP       = 420f;
            damage      = 22f;
            isElderFlag = true; // This IS the Elder Flag boss

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();

            // 40x44 sprite scale
            transform.localScale = new Vector3(40f / 32f, 44f / 32f, 1f);

            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;

            // Show Easter egg subtitle
            SetupEasterEggSubtitle();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _spiritTimer   -= Time.deltaTime;
            _splitterTimer -= Time.deltaTime;

            // Natural Order aura check every frame
            CheckNaturalOrder();

            // Attack priority: Earth Splitter > Astral Spirit > move
            if (_splitterTimer <= 0f)
            {
                StartCoroutine(PerformEarthSplitter());
                return;
            }

            if (_spiritTimer <= 0f)
            {
                StartCoroutine(PerformAstralSpirit());
                return;
            }

            if (DistanceToPlayer > 2f)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Easter Egg Subtitle ────────────────────────────────────────────

        private void SetupEasterEggSubtitle()
        {
            if (easterEggSubtitleText != null)
            {
                easterEggSubtitleText.text = "Easter egg by Lucker23";
                easterEggSubtitleText.color = new Color(0.10f, 0.90f, 0.85f, 0.85f);
                easterEggSubtitleText.gameObject.SetActive(true);
            }

            // Also attempt to find it on the health bar if not directly assigned
            if (easterEggSubtitleText == null && bossHealthBar != null)
            {
                TextMeshProUGUI[] texts = bossHealthBar.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (t.gameObject.name.ToLower().Contains("subtitle") ||
                        t.gameObject.name.ToLower().Contains("easter"))
                    {
                        t.text = "Easter egg by Lucker23";
                        t.color = new Color(0.10f, 0.90f, 0.85f, 0.85f);
                        t.gameObject.SetActive(true);
                        easterEggSubtitleText = t;
                        break;
                    }
                }
            }
        }

        // ── Attack 1: Astral Spirit ────────────────────────────────────────

        /// <summary>
        /// Sends a ghost projection forward in a boomerang path.
        /// Damages on both outward and return passes.
        /// Phase 2: leaves a damaging trail.
        /// </summary>
        private IEnumerator PerformAstralSpirit()
        {
            _isActing    = true;
            _spiritTimer = spiritCooldown;

            if (animator != null) animator.SetTrigger("SpiritLaunch");

            if (astralSpiritPrefab == null)
            {
                yield return new WaitForSeconds(0.5f);
                _isActing = false;
                yield break;
            }

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;

            GameObject spirit = Instantiate(astralSpiritPrefab, transform.position, Quaternion.identity);
            AstralSpiritProjectile proj = spirit.GetComponent<AstralSpiritProjectile>();

            if (proj == null)
                proj = spirit.AddComponent<AstralSpiritProjectile>();

            proj.Initialise(transform.position, facingX, spiritSpeed, spiritRange,
                damage * 0.7f, phase2SpiritTrail, playerLayer, gameObject);

            // Wait for spirit to complete round trip (estimated)
            float travelTime = (spiritRange / spiritSpeed) * 2.2f;
            yield return new WaitForSeconds(travelTime + 0.3f);

            _isActing = false;
        }

        // ── Attack 2: Earth Splitter ───────────────────────────────────────

        /// <summary>
        /// 1.5s charge with visible crack line on ground, then massive line damage.
        /// Phase 2: wider hitbox.
        /// </summary>
        private IEnumerator PerformEarthSplitter()
        {
            _isActing      = true;
            _splitterTimer = splitterCooldown;

            if (animator != null) animator.SetTrigger("Charge");

            // Determine direction toward player
            float facingX = 1f;
            if (playerTransform != null)
            {
                facingX = Mathf.Sign(playerTransform.position.x - transform.position.x);
                if (spriteRenderer != null) spriteRenderer.flipX = facingX < 0f;
            }

            // Spawn crack preview line
            GameObject crack = null;
            if (crackLinePrefab != null)
            {
                crack = Instantiate(crackLinePrefab,
                    transform.position + Vector3.right * facingX * splitterLength * 0.5f,
                    Quaternion.identity);
                crack.transform.localScale = new Vector3(splitterLength, splitterWidth, 1f);
            }

            // Show telegraph
            yield return StartCoroutine(_ai.TelegraphAttack(splitterChargeTime,
                transform.position + Vector3.right * facingX * splitterLength * 0.5f));

            if (crack != null) Destroy(crack);

            // Strike – spawn hitbox along the ground
            if (splitterHitboxPrefab != null)
            {
                Vector3 hitboxCenter = transform.position + Vector3.right * facingX * splitterLength * 0.5f;
                GameObject hitbox = Instantiate(splitterHitboxPrefab, hitboxCenter, Quaternion.identity);
                hitbox.transform.localScale = new Vector3(splitterLength, splitterWidth, 1f);

                // Damage everything in the line
                Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter,
                    new Vector2(splitterLength, splitterWidth), 0f, playerLayer);
                foreach (Collider2D h in hits)
                {
                    IDamageable dmg = h.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, damage * 1.5f,
                            new Vector2(facingX * 3f, 5f), DamageType.Normal, gameObject);
                }

                Destroy(hitbox, 0.3f);
            }

            if (animator != null) animator.SetTrigger("Strike");

            yield return new WaitForSeconds(0.8f);
            _isActing = false;
        }

        // ── Attack 3: Natural Order ────────────────────────────────────────

        /// <summary>
        /// Passive aura: reduces player defense by 30% when within 4 units.
        /// Applied via a simple proximity check; signals to a PlayerStats component.
        /// </summary>
        private void CheckNaturalOrder()
        {
            if (playerTransform == null) return;

            bool inRange = DistanceToPlayer <= naturalOrderRadius;

            if (inRange && !_naturalOrderActive)
            {
                _naturalOrderActive = true;
                ApplyNaturalOrderDebuff(true);
            }
            else if (!inRange && _naturalOrderActive)
            {
                _naturalOrderActive = false;
                ApplyNaturalOrderDebuff(false);
            }
        }

        private void ApplyNaturalOrderDebuff(bool apply)
        {
            if (playerTransform == null) return;

            // Broadcast to any component listening on player that handles defense modification
            PlayerDebuffReceiver receiver = playerTransform.GetComponent<PlayerDebuffReceiver>();
            if (receiver != null)
            {
                if (apply)
                    receiver.ApplyDefenseReduction(defenseReductionPercent, "NaturalOrder");
                else
                    receiver.RemoveDefenseReduction("NaturalOrder");
            }
        }

        protected override void HandleOnBossDefeated()
        {
            // Remove Natural Order debuff on death
            if (_naturalOrderActive)
                ApplyNaturalOrderDebuff(false);
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0f, 0.9f, 0.9f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, naturalOrderRadius);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Drives the Astral Spirit projectile in a boomerang path.
    /// Damages on outward pass and return pass.
    /// </summary>
    public class AstralSpiritProjectile : MonoBehaviour
    {
        private float _speed;
        private float _range;
        private float _damage;
        private float _traveled;
        private float _direction;
        private bool _returning;
        private bool _phase2Trail;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private Vector3 _origin;

        private HashSet<Collider2D> _hitSet = new HashSet<Collider2D>();

        public void Initialise(Vector3 origin, float dir, float speed, float range,
            float damage, bool phase2Trail, LayerMask playerLayer, GameObject owner)
        {
            _origin      = origin;
            _direction   = dir;
            _speed       = speed;
            _range       = range;
            _damage      = damage;
            _phase2Trail = phase2Trail;
            _playerLayer = playerLayer;
            _owner       = owner;
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            float moveDir = _returning ? -_direction : _direction;

            transform.position += Vector3.right * moveDir * step;
            _traveled += step;

            // Leave damaging trail in phase 2
            if (_phase2Trail)
                CheckTrailDamage();

            if (!_returning && _traveled >= _range)
            {
                _returning = true;
                _traveled  = 0f;
                _hitSet.Clear(); // Allow hitting again on return
            }
            else if (_returning && Vector3.Distance(transform.position, _origin) < 0.5f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hitSet.Contains(other)) return;

            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) == 0) return;

            _hitSet.Add(other);
            IDamageable dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
                DamageSystem.Instance?.DealDamage(dmg, _damage, Vector2.right * _direction * 3f,
                    DamageType.Normal, _owner);
        }

        private void CheckTrailDamage()
        {
            Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.4f, _playerLayer);
            if (hit != null && !_hitSet.Contains(hit))
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, _damage * 0.3f * Time.deltaTime,
                        Vector2.zero, DamageType.Normal, _owner);
            }
        }
    }

    /// <summary>
    /// Stub receiver on the player that handles defense-reduction debuffs.
    /// If a full PlayerStats system is present, this bridges into it.
    /// </summary>
    public class PlayerDebuffReceiver : MonoBehaviour
    {
        private System.Collections.Generic.Dictionary<string, float> _defenseDebuffs
            = new System.Collections.Generic.Dictionary<string, float>();

        public void ApplyDefenseReduction(float fraction, string key)
        {
            _defenseDebuffs[key] = fraction;
        }

        public void RemoveDefenseReduction(string key)
        {
            _defenseDebuffs.Remove(key);
        }

        /// <summary>Returns the combined defense multiplier (e.g., 0.7 means 30% less defense).</summary>
        public float GetDefenseMultiplier()
        {
            float total = 1f;
            foreach (var v in _defenseDebuffs.Values)
                total *= (1f - v);
            return Mathf.Clamp01(total);
        }
    }
}
