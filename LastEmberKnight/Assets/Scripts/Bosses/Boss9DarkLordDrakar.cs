using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 9 – Dark Lord Drakar (Zone 9, Level 49).
    /// THE FINAL BOSS.
    /// An apocalyptic war of attrition against a fiery demigod.
    /// 800 HP / 40 damage / 38x46 size.
    ///
    /// Phase 2:
    ///   - Doom also disables Dash
    ///   - Summons 2 AshenGuard minions every 20s
    ///   - Fire covers 60% of the arena
    ///
    /// Visual: dark red / black / orange flames.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss9DarkLordDrakar : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Dark Lord Drakar – Stats")]
        [SerializeField] private float moveSpeed = 3.0f;

        // Attack 1 – Doom
        [Header("Doom")]
        [SerializeField] private float doomDPS = 5f;
        [SerializeField] private float doomDuration = 8f;
        [SerializeField] private float doomCooldown = 10f;
        [SerializeField] private GameObject doomDebuffVFXPrefab;
        [SerializeField] private float doomRadius = 5f;

        // Attack 2 – Devour
        [Header("Devour")]
        [SerializeField] private float devourHeal = 50f;
        [SerializeField] private float devourChargeDist = 8f;
        [SerializeField] private float devourChargeSpeed = 12f;
        [SerializeField] private float devourCooldown = 8f;

        // Attack 3 – Scorched Earth
        [Header("Scorched Earth")]
        [SerializeField] private float scorchedEarthWidth = 10f;
        [SerializeField] private float scorchedEarthDPS = 12f;
        [SerializeField] private float scorchedEarthDuration = 5f;
        [SerializeField] private float scorchedEarthHealPerSecond = 8f;
        [SerializeField] private float scorchedEarthCooldown = 12f;
        [SerializeField] private GameObject scorchedEarthPrefab;

        // Attack 4 – Infernal Blade
        [Header("Infernal Blade")]
        [SerializeField] private float infernalBladeDOT = 6f;
        [SerializeField] private float infernalBladeDOTDuration = 4f;
        [SerializeField] private float meleeRange = 1.8f;
        [SerializeField] private float meleeCooldown = 1.3f;

        // Phase 2 – Minions
        [Header("Phase 2 – Minions")]
        [SerializeField] private GameObject ashenGuardPrefab;
        [SerializeField] private float minionSummonInterval = 20f;
        [SerializeField] private int minionsPerSummon = 2;
        [SerializeField] private float minionSpawnOffsetX = 3f;

        // Phase 2 – Arena Fire
        [Header("Phase 2 – Arena Fire")]
        [SerializeField] private float arenaFireCoverage = 0.60f;  // 60% of arena width
        [SerializeField] private float arenaFireDPS = 10f;
        [SerializeField] private GameObject arenaFirePrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.45f, 0.04f, 0.04f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.60f, 0.04f, 0.01f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _doomTimer;
        private float _devourTimer;
        private float _scorchedTimer;
        private float _meleeTimer;
        private float _minionSummonTimer;
        private bool _isActing;

        // DOT tracking: player can have Doom burning debuff
        private bool _doomActive;
        private Coroutine _doomCoroutine;

        // Scorched Earth: heal boss while standing in it
        private bool _inScorchedEarth;
        private List<GameObject> _arenaFireZones = new List<GameObject>();

        // Devour buffs
        private enum DevourBuff { Speed, Damage, Armor }

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Dark Lord Drakar";

        protected override void OnPhaseTransition()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            _minionSummonTimer = 3f; // First summon quickly after phase 2
            SpawnArenaFire();

            Debug.Log("[DarkLordDrakar] Phase 2 – The arena is consumed by fire! Minions join the battle!");
        }

        protected override void ExecuteAttack() { }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Dark Lord Drakar";
            bossIndex   = 9;
            maxHP       = 800f;
            damage      = 40f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(38f / 32f, 46f / 32f, 1f);

            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing) return;

            _doomTimer       -= Time.deltaTime;
            _devourTimer     -= Time.deltaTime;
            _scorchedTimer   -= Time.deltaTime;
            _meleeTimer      -= Time.deltaTime;

            if (phase == 2)
            {
                _minionSummonTimer -= Time.deltaTime;
                if (_minionSummonTimer <= 0f)
                {
                    _minionSummonTimer = minionSummonInterval;
                    SummonMinions();
                }

                // Heal while standing in Scorched Earth
                if (_inScorchedEarth)
                    currentHP = Mathf.Min(currentHP + scorchedEarthHealPerSecond * Time.deltaTime, maxHP);
            }

            float dist = DistanceToPlayer;

            // Attack priority: Doom > Scorched Earth > Devour > Melee/Infernal Blade
            if (_doomTimer <= 0f && dist <= doomRadius)
            {
                StartCoroutine(CastDoom());
                return;
            }

            if (_scorchedTimer <= 0f)
            {
                StartCoroutine(CastScorchedEarth());
                return;
            }

            if (_devourTimer <= 0f && dist <= devourChargeDist)
            {
                StartCoroutine(CastDevour());
                return;
            }

            if (_meleeTimer <= 0f && dist <= meleeRange)
            {
                StartCoroutine(PerformInfernalBladeStrike());
                return;
            }

            if (dist > meleeRange)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Attack 1: Doom ─────────────────────────────────────────────────

        /// <summary>
        /// Marks the player with burning debuff (5 DPS for 8s) AND silences skills.
        /// Phase 2: also disables Dash.
        /// </summary>
        private IEnumerator CastDoom()
        {
            _isActing  = true;
            _doomTimer = doomCooldown;

            if (animator != null) animator.SetTrigger("Doom");
            yield return new WaitForSeconds(0.4f);

            if (playerTransform != null)
            {
                // Apply debuff
                DoomDebuffReceiver doomReceiver = playerTransform.GetComponent<DoomDebuffReceiver>();
                if (doomReceiver == null)
                    doomReceiver = playerTransform.gameObject.AddComponent<DoomDebuffReceiver>();

                bool disableDash = (phase == 2);
                doomReceiver.ApplyDoom(doomDPS, doomDuration, disableDash);

                if (doomDebuffVFXPrefab != null)
                {
                    GameObject vfx = Instantiate(doomDebuffVFXPrefab,
                        playerTransform.position, Quaternion.identity);
                    vfx.transform.SetParent(playerTransform);
                    Destroy(vfx, doomDuration);
                }

                Debug.Log($"[Drakar] Doom cast – player burning {doomDPS} DPS for {doomDuration}s" +
                    (disableDash ? " + Dash disabled!" : ""));
            }

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        // ── Attack 2: Devour ───────────────────────────────────────────────

        /// <summary>
        /// Charges at the player. If hit: heals 50 HP + gains a random buff.
        /// </summary>
        private IEnumerator CastDevour()
        {
            _isActing    = true;
            _devourTimer = devourCooldown;

            if (animator != null) animator.SetTrigger("Devour");

            // Telegraph
            yield return StartCoroutine(_ai.TelegraphAttack(0.4f, transform.position));

            Vector3 chargeTarget = playerTransform != null
                ? playerTransform.position
                : transform.position + Vector3.right * 4f;

            float facingX = Mathf.Sign(chargeTarget.x - transform.position.x);
            if (spriteRenderer != null) spriteRenderer.flipX = facingX < 0f;

            // Charge
            float chargeTime  = Vector3.Distance(transform.position, chargeTarget) / devourChargeSpeed;
            float elapsed     = 0f;
            Vector3 startPos  = transform.position;
            bool hitPlayer    = false;
            HashSet<Collider2D> hit = new HashSet<Collider2D>();

            while (elapsed < chargeTime)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, chargeTarget, elapsed / chargeTime);

                Collider2D playerCol = Physics2D.OverlapCircle(
                    (Vector2)transform.position, 1.2f, playerLayer);
                if (playerCol != null && !hit.Contains(playerCol))
                {
                    hit.Add(playerCol);
                    IDamageable dmg = playerCol.GetComponent<IDamageable>();
                    if (dmg != null)
                        DamageSystem.Instance?.DealDamage(dmg, damage * 1.2f,
                            new Vector2(facingX * 7f, 4f), DamageType.Normal, gameObject);

                    hitPlayer = true;
                }

                yield return null;
            }

            // If charge connected: heal + random buff
            if (hitPlayer)
            {
                currentHP = Mathf.Min(currentHP + devourHeal, maxHP);
                ApplyDevourBuff();
                Debug.Log("[Drakar] Devour connected – healed and gained a buff!");
            }

            yield return new WaitForSeconds(0.4f);
            _isActing = false;
        }

        private void ApplyDevourBuff()
        {
            DevourBuff buff = (DevourBuff)Random.Range(0, 3);
            switch (buff)
            {
                case DevourBuff.Speed:
                    moveSpeed *= 1.25f;
                    StartCoroutine(RemoveBuffAfter(() => moveSpeed /= 1.25f, 8f));
                    Debug.Log("[Drakar] Devour Buff: SPEED");
                    break;
                case DevourBuff.Damage:
                    damage *= 1.25f;
                    StartCoroutine(RemoveBuffAfter(() => damage /= 1.25f, 8f));
                    Debug.Log("[Drakar] Devour Buff: DAMAGE");
                    break;
                case DevourBuff.Armor:
                    // Increase invincibility window briefly as "armor"
                    invincibilityDuration += 0.2f;
                    StartCoroutine(RemoveBuffAfter(() => invincibilityDuration -= 0.2f, 8f));
                    Debug.Log("[Drakar] Devour Buff: ARMOR");
                    break;
            }
        }

        private IEnumerator RemoveBuffAfter(System.Action remove, float delay)
        {
            yield return new WaitForSeconds(delay);
            remove?.Invoke();
        }

        // ── Attack 3: Scorched Earth ───────────────────────────────────────

        private IEnumerator CastScorchedEarth()
        {
            _isActing      = true;
            _scorchedTimer = scorchedEarthCooldown;

            if (animator != null) animator.SetTrigger("ScorchedEarth");
            yield return new WaitForSeconds(0.4f);

            if (scorchedEarthPrefab == null)
            {
                yield return new WaitForSeconds(0.5f);
                _isActing = false;
                yield break;
            }

            Vector3 zonePos = transform.position;
            GameObject zone = Instantiate(scorchedEarthPrefab, zonePos, Quaternion.identity);
            zone.transform.localScale = new Vector3(scorchedEarthWidth, 1.5f, 1f);

            // Apply damage zone to players
            BurningGroundZone fireZone = zone.GetComponent<BurningGroundZone>();
            if (fireZone == null) fireZone = zone.AddComponent<BurningGroundZone>();
            fireZone.Initialise(scorchedEarthDuration, scorchedEarthDPS, playerLayer, gameObject);

            // Boss heals while in the zone
            StartCoroutine(HealWhileInScorchedEarth(zone, scorchedEarthDuration));

            Destroy(zone, scorchedEarthDuration + 0.1f);

            yield return new WaitForSeconds(0.5f);
            _isActing = false;
        }

        private IEnumerator HealWhileInScorchedEarth(GameObject zone, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && zone != null)
            {
                elapsed += Time.deltaTime;
                if (zone != null)
                {
                    bool inside = Vector2.Distance(transform.position,
                        zone.transform.position) <= scorchedEarthWidth * 0.5f;
                    _inScorchedEarth = inside;
                }
                yield return null;
            }
            _inScorchedEarth = false;
        }

        // ── Attack 4: Infernal Blade (Melee DOT) ──────────────────────────

        private IEnumerator PerformInfernalBladeStrike()
        {
            _isActing   = true;
            _meleeTimer = meleeCooldown;

            if (animator != null) animator.SetTrigger("InfernalBlade");
            yield return new WaitForSeconds(0.18f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(1.8f, 1.6f), 0f, playerLayer);

            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(facingX * 4f, 2f), DamageType.Normal, gameObject);

                // Apply burning DOT via DoomDebuffReceiver (reuse for DOT)
                DoomDebuffReceiver dotReceiver = hit.GetComponent<DoomDebuffReceiver>();
                if (dotReceiver == null)
                    dotReceiver = hit.gameObject.AddComponent<DoomDebuffReceiver>();
                dotReceiver.ApplyBurningDOT(infernalBladeDOT, infernalBladeDOTDuration);
            }

            yield return new WaitForSeconds(0.4f);
            _isActing = false;
        }

        // ── Phase 2: Summon Minions ────────────────────────────────────────

        private void SummonMinions()
        {
            if (ashenGuardPrefab == null) return;

            if (animator != null) animator.SetTrigger("Summon");

            for (int i = 0; i < minionsPerSummon; i++)
            {
                float side  = (i == 0) ? 1f : -1f;
                Vector3 pos = transform.position + Vector3.right * side * minionSpawnOffsetX;
                GameObject minion = Instantiate(ashenGuardPrefab, pos, Quaternion.identity);
                Debug.Log($"[Drakar] Summoned AshenGuard at {pos}");
            }
        }

        // ── Phase 2: Arena Fire ────────────────────────────────────────────

        private void SpawnArenaFire()
        {
            if (arenaFirePrefab == null) return;

            // Cover 60% of arena width with fire
            float fireWidth = _ai.ArenaWidth * arenaFireCoverage;

            // Split into left and right sections (leaving a gap in center for player to dodge)
            float halfFire = fireWidth * 0.5f;
            float gapX     = _ai.ArenaWidth * 0.20f; // 20% gap in center

            // Left fire zone
            Vector3 leftPos = new Vector3(_ai.ArenaCenter.x - gapX - halfFire * 0.5f,
                _ai.ArenaCenter.y - 3.5f, 0f);
            SpawnArenaFireZone(leftPos, new Vector3(halfFire, 1.5f, 1f));

            // Right fire zone
            Vector3 rightPos = new Vector3(_ai.ArenaCenter.x + gapX + halfFire * 0.5f,
                _ai.ArenaCenter.y - 3.5f, 0f);
            SpawnArenaFireZone(rightPos, new Vector3(halfFire, 1.5f, 1f));
        }

        private void SpawnArenaFireZone(Vector3 pos, Vector3 scale)
        {
            GameObject zone = Instantiate(arenaFirePrefab, pos, Quaternion.identity);
            zone.transform.localScale = scale;
            _arenaFireZones.Add(zone);

            BurningGroundZone burnZone = zone.GetComponent<BurningGroundZone>();
            if (burnZone == null) burnZone = zone.AddComponent<BurningGroundZone>();
            // This zone is permanent (large duration), destroyed when boss dies
            burnZone.Initialise(999f, arenaFireDPS, playerLayer, gameObject);
        }

        protected override void HandleOnBossDefeated()
        {
            // Clean up arena fire on boss death
            foreach (GameObject zone in _arenaFireZones)
            {
                if (zone != null)
                    Destroy(zone);
            }
            _arenaFireZones.Clear();

            // Remove Doom from player
            if (playerTransform != null)
            {
                DoomDebuffReceiver doom = playerTransform.GetComponent<DoomDebuffReceiver>();
                doom?.ClearAllDebuffs();
            }
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, doomRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, devourChargeDist);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Handles Doom debuffs and burning DOT effects on the player.
    /// Communicates silence and dash-disable to PlayerController / ability systems.
    /// </summary>
    public class DoomDebuffReceiver : MonoBehaviour
    {
        // Doom state
        private bool _doomActive;
        private bool _skillsSilenced;
        private bool _dashDisabled;

        // DOT coroutines tracked per source
        private readonly Dictionary<string, Coroutine> _dotCoroutines
            = new Dictionary<string, Coroutine>();

        public bool IsSkillsSilenced => _skillsSilenced;
        public bool IsDashDisabled   => _dashDisabled;

        /// <summary>Applies the full Doom effect: burning DPS, silence, optional dash disable.</summary>
        public void ApplyDoom(float dps, float duration, bool disableDash)
        {
            if (_doomActive)
            {
                // Refresh
                ClearDoom();
            }

            _doomActive      = true;
            _skillsSilenced  = true;
            _dashDisabled    = disableDash;

            ApplyBurningDOT(dps, duration, "Doom");
            StartCoroutine(DoomDurationCoroutine(duration));
        }

        private IEnumerator DoomDurationCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            ClearDoom();
        }

        private void ClearDoom()
        {
            _doomActive     = false;
            _skillsSilenced = false;
            _dashDisabled   = false;
        }

        /// <summary>Applies a burning DOT with the given key (for stacking prevention).</summary>
        public void ApplyBurningDOT(float dps, float duration, string key = "InfernalBlade")
        {
            if (_dotCoroutines.TryGetValue(key, out Coroutine existing) && existing != null)
                StopCoroutine(existing);

            Coroutine c = StartCoroutine(BurningDOTCoroutine(dps, duration, key));
            _dotCoroutines[key] = c;
        }

        private IEnumerator BurningDOTCoroutine(float dps, float duration, string key)
        {
            float elapsed = 0f;
            IDamageable self = GetComponent<IDamageable>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (self != null)
                    DamageSystem.Instance?.DealDamage(self, dps * Time.deltaTime,
                        Vector2.zero, DamageType.Normal, null);
                yield return null;
            }

            if (_dotCoroutines.ContainsKey(key))
                _dotCoroutines.Remove(key);
        }

        /// <summary>Removes all active debuffs (called on boss death).</summary>
        public void ClearAllDebuffs()
        {
            StopAllCoroutines();
            _dotCoroutines.Clear();
            ClearDoom();
        }
    }
}
