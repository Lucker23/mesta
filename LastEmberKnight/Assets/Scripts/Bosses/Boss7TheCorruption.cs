using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 7 – The Corruption (Zone 7, Level 39).
    /// THE HARDEST REGULAR BOSS.
    /// An ancient sorcerer cycling through elemental combinations.
    /// 500 HP / 35 damage / 40x40 size.
    /// Has 3 orbiting elements: Fire, Ice, Void.
    /// Cycles spell combinations every 4s.
    /// Phase 2: casts TWO spells simultaneously, orbs rotate faster.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss7TheCorruption : BossBase
    {
        // ── Element Enum ──────────────────────────────────────────────────

        public enum Element { Fire, Ice, Void }

        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("The Corruption – Stats")]
        [SerializeField] private float moveSpeed = 2.2f;

        [Header("Orbiting Elements")]
        [SerializeField] private float orbitRadius = 1.8f;
        [SerializeField] private float orbitSpeed = 90f;        // degrees/s
        [SerializeField] private float phase2OrbitSpeed = 180f;
        [SerializeField] private GameObject fireOrbPrefab;
        [SerializeField] private GameObject iceOrbPrefab;
        [SerializeField] private GameObject voidOrbPrefab;

        [Header("Spell Cycle")]
        [SerializeField] private float spellCycleInterval = 4f;

        [Header("FFF – Sunstrike")]
        [SerializeField] private float sunstrikeWarningTime = 3f;
        [SerializeField] private float sunstrikeWidth = 1.5f;
        [SerializeField] private GameObject sunstrikeWarningPrefab;
        [SerializeField] private GameObject sunstrikePrefab;

        [Header("III – Ice Wall")]
        [SerializeField] private float iceWallDuration = 3f;
        [SerializeField] private float iceWallWidth = 1.5f;
        [SerializeField] private float iceWallHeight = 4f;
        [SerializeField] private GameObject iceWallPrefab;

        [Header("VVV – EMP")]
        [SerializeField] private float empManaDrain = 30f;
        [SerializeField] private float empRadius = 6f;
        [SerializeField] private GameObject empParticlesPrefab;

        [Header("FIV – Chaos Meteor")]
        [SerializeField] private float meteorSpeed = 5f;
        [SerializeField] private float meteorDamage = 1.3f;
        [SerializeField] private GameObject meteorPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.25f, 0.05f, 0.35f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.40f, 0.02f, 0.55f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _spellCycleTimer;
        private bool _isCasting;

        // Orb visual instances
        private GameObject _fireOrb;
        private GameObject _iceOrb;
        private GameObject _voidOrb;
        private float _currentOrbitAngle;

        // Current elemental combination (3 slots)
        private Element[] _currentCombo = new Element[3];

        // Combination cycle index
        private int _comboIndex;

        // Predefined combination cycles
        private static readonly Element[][] _comboCycle = new Element[][]
        {
            new Element[] { Element.Fire,  Element.Fire,  Element.Fire  }, // FFF Sunstrike
            new Element[] { Element.Ice,   Element.Ice,   Element.Ice   }, // III Ice Wall
            new Element[] { Element.Void,  Element.Void,  Element.Void  }, // VVV EMP
            new Element[] { Element.Fire,  Element.Ice,   Element.Void  }, // FIV Chaos Meteor
        };

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "The Corruption";

        protected override void OnPhaseTransition()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[TheCorruption] Phase 2 – Dual spells, faster orb rotation!");
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "The Corruption";
            bossIndex   = 7;
            maxHP       = 500f;
            damage      = 35f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(40f / 32f, 40f / 32f, 1f);

            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;

            SpawnOrbVisuals();
            _currentCombo = _comboCycle[0];
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive) return;

            UpdateOrbPositions();

            if (_isCasting) return;

            _spellCycleTimer -= Time.deltaTime;

            if (_spellCycleTimer <= 0f)
            {
                _spellCycleTimer = spellCycleInterval;
                AdvanceCombo();
                CastCurrentCombo();
            }

            if (DistanceToPlayer > 2f)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Orb Visuals ────────────────────────────────────────────────────

        private void SpawnOrbVisuals()
        {
            if (fireOrbPrefab != null)
            {
                _fireOrb = Instantiate(fireOrbPrefab, transform.position, Quaternion.identity);
                _fireOrb.transform.SetParent(transform);
            }
            if (iceOrbPrefab != null)
            {
                _iceOrb = Instantiate(iceOrbPrefab, transform.position, Quaternion.identity);
                _iceOrb.transform.SetParent(transform);
            }
            if (voidOrbPrefab != null)
            {
                _voidOrb = Instantiate(voidOrbPrefab, transform.position, Quaternion.identity);
                _voidOrb.transform.SetParent(transform);
            }
        }

        private void UpdateOrbPositions()
        {
            float speed = (phase == 2) ? phase2OrbitSpeed : orbitSpeed;
            _currentOrbitAngle += speed * Time.deltaTime;

            UpdateSingleOrb(_fireOrb, _currentOrbitAngle);
            UpdateSingleOrb(_iceOrb,  _currentOrbitAngle + 120f);
            UpdateSingleOrb(_voidOrb, _currentOrbitAngle + 240f);
        }

        private void UpdateSingleOrb(GameObject orb, float angle)
        {
            if (orb == null) return;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                Mathf.Sin(rad) * orbitRadius * 0.6f,
                0f);
            orb.transform.position = transform.position + offset;
        }

        // ── Combo Cycle ────────────────────────────────────────────────────

        private void AdvanceCombo()
        {
            _comboIndex = (_comboIndex + 1) % _comboCycle.Length;
            _currentCombo = _comboCycle[_comboIndex];
        }

        private void CastCurrentCombo()
        {
            if (phase == 2)
            {
                // Cast TWO spells simultaneously in phase 2
                StartCoroutine(CastSpell(_currentCombo));
                int nextComboIdx = (_comboIndex + 1) % _comboCycle.Length;
                StartCoroutine(CastSpell(_comboCycle[nextComboIdx]));
            }
            else
            {
                StartCoroutine(CastSpell(_currentCombo));
            }
        }

        private IEnumerator CastSpell(Element[] combo)
        {
            _isCasting = true;

            string comboKey = $"{combo[0]}{combo[1]}{combo[2]}";

            if (combo[0] == Element.Fire && combo[1] == Element.Fire && combo[2] == Element.Fire)
            {
                yield return StartCoroutine(CastSunstrike());
            }
            else if (combo[0] == Element.Ice && combo[1] == Element.Ice && combo[2] == Element.Ice)
            {
                yield return StartCoroutine(CastIceWall());
            }
            else if (combo[0] == Element.Void && combo[1] == Element.Void && combo[2] == Element.Void)
            {
                yield return StartCoroutine(CastEMP());
            }
            else if (combo[0] == Element.Fire && combo[1] == Element.Ice && combo[2] == Element.Void)
            {
                yield return StartCoroutine(CastChaosMeteor());
            }

            _isCasting = false;
        }

        // ── FFF – Sunstrike ────────────────────────────────────────────────

        private IEnumerator CastSunstrike()
        {
            if (animator != null) animator.SetTrigger("Sunstrike");

            // Warning circle at player's position
            Vector3 targetPos = playerTransform != null
                ? playerTransform.position
                : transform.position + Vector3.right * 2f;

            GameObject warning = null;
            if (sunstrikeWarningPrefab != null)
            {
                warning = Instantiate(sunstrikeWarningPrefab, targetPos, Quaternion.identity);
                warning.transform.localScale = new Vector3(sunstrikeWidth, 6f, 1f);
            }

            yield return new WaitForSeconds(sunstrikeWarningTime);

            if (warning != null) Destroy(warning);

            // Strike column from sky
            if (sunstrikePrefab != null)
            {
                GameObject strike = Instantiate(sunstrikePrefab, targetPos, Quaternion.identity);
                strike.transform.localScale = new Vector3(sunstrikeWidth, 8f, 1f);

                Collider2D[] hits = Physics2D.OverlapBoxAll(
                    (Vector2)targetPos, new Vector2(sunstrikeWidth, 8f), 0f, playerLayer);
                foreach (Collider2D c in hits)
                {
                    IDamageable dmg = c.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, damage * 1.4f,
                            Vector2.down * 5f, DamageType.Normal, gameObject);
                }

                Destroy(strike, 0.4f);
            }

            yield return new WaitForSeconds(0.5f);
        }

        // ── III – Ice Wall ─────────────────────────────────────────────────

        private IEnumerator CastIceWall()
        {
            if (animator != null) animator.SetTrigger("IceWall");
            yield return new WaitForSeconds(0.3f);

            // Spawn wall between boss and player
            Vector3 wallPos = playerTransform != null
                ? Vector3.Lerp(transform.position, playerTransform.position, 0.5f)
                : transform.position + Vector3.right * 3f;

            GameObject wall = null;
            if (iceWallPrefab != null)
            {
                wall = Instantiate(iceWallPrefab, wallPos, Quaternion.identity);
                wall.transform.localScale = new Vector3(iceWallWidth, iceWallHeight, 1f);

                // Ensure it has a solid collider to block player
                BoxCollider2D wallCol = wall.GetComponent<BoxCollider2D>();
                if (wallCol == null)
                {
                    wallCol = wall.AddComponent<BoxCollider2D>();
                    wallCol.size = Vector2.one;
                }
                wallCol.isTrigger = false;
            }

            yield return new WaitForSeconds(iceWallDuration);

            if (wall != null) Destroy(wall);

            yield return new WaitForSeconds(0.3f);
        }

        // ── VVV – EMP ──────────────────────────────────────────────────────

        private IEnumerator CastEMP()
        {
            if (animator != null) animator.SetTrigger("EMP");

            if (empParticlesPrefab != null)
            {
                GameObject fx = Instantiate(empParticlesPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 1.5f);
            }

            yield return new WaitForSeconds(0.4f);

            // Drain player mana
            if (playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                if (dist <= empRadius)
                {
                    PlayerManaReceiver mana = playerTransform.GetComponent<PlayerManaReceiver>();
                    if (mana != null)
                        mana.DrainMana(empManaDrain);

                    Debug.Log($"[TheCorruption] EMP drained {empManaDrain} mana from player.");
                }
            }

            yield return new WaitForSeconds(0.4f);
        }

        // ── FIV – Chaos Meteor ─────────────────────────────────────────────

        private IEnumerator CastChaosMeteor()
        {
            if (animator != null) animator.SetTrigger("Meteor");
            yield return new WaitForSeconds(0.3f);

            // Spawn rolling fireball at the boss and send it across the floor
            if (meteorPrefab == null)
            {
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector3 spawnPos = transform.position + Vector3.right * facingX;
            GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

            RollingMeteor meteScript = meteor.GetComponent<RollingMeteor>();
            if (meteScript == null) meteScript = meteor.AddComponent<RollingMeteor>();
            meteScript.Initialise(facingX, meteorSpeed, damage * meteorDamage,
                playerLayer, gameObject, _ai.ArenaWidth);

            yield return new WaitForSeconds(0.5f);
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.8f, 0.1f, 0.9f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, empRadius);
            Gizmos.color = new Color(0.5f, 0f, 0.7f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, orbitRadius);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Rolling fireball that travels along the ground for Chaos Meteor.
    /// </summary>
    public class RollingMeteor : MonoBehaviour
    {
        private float _dir;
        private float _speed;
        private float _damage;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private float _arenaWidth;
        private float _traveled;
        private bool _hit;

        public void Initialise(float dir, float speed, float damage,
            LayerMask playerLayer, GameObject owner, float arenaWidth)
        {
            _dir        = dir;
            _speed      = speed;
            _damage     = damage;
            _playerLayer = playerLayer;
            _owner      = owner;
            _arenaWidth = arenaWidth;

            Destroy(gameObject, arenaWidth / speed + 1f);
        }

        private void Update()
        {
            if (_hit) return;
            transform.position += Vector3.right * _dir * _speed * Time.deltaTime;
            transform.Rotate(0f, 0f, _dir * -120f * Time.deltaTime);
            _traveled += _speed * Time.deltaTime;
            if (_traveled >= _arenaWidth) Destroy(gameObject);
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
                    new Vector2(_dir * 5f, 4f), DamageType.Normal, _owner);

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Stub receiver for mana drain effects on the player.
    /// Bridges into full player mana system if available.
    /// </summary>
    public class PlayerManaReceiver : MonoBehaviour
    {
        [SerializeField] private float mana = 50f;
        [SerializeField] private float maxMana = 50f;

        public float Mana    => mana;
        public float MaxMana => maxMana;

        public void DrainMana(float amount)
        {
            mana = Mathf.Max(0f, mana - amount);
        }

        public void RestoreMana(float amount)
        {
            mana = Mathf.Min(maxMana, mana + amount);
        }
    }
}
