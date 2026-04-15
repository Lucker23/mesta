using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LastEmberKnight
{
    // =========================================================================
    //  Upgrade type enum
    // =========================================================================

    public enum UpgradeType
    {
        MaxHP       = 0,
        Attack      = 1,
        DashCD      = 2,
        SoulRate    = 3,
        MaxMana     = 4,
    }

    // =========================================================================
    //  UpgradeSystem
    // =========================================================================

    /// <summary>
    /// Singleton that tracks all purchased upgrades, applies stat changes to the
    /// player, and persists data via <see cref="SaveSystem"/>.
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static UpgradeSystem Instance { get; private set; }

        // ── Cost table ────────────────────────────────────────────────────────
        private static readonly Dictionary<UpgradeType, int> CostTable =
            new Dictionary<UpgradeType, int>
            {
                { UpgradeType.MaxHP,    5 },
                { UpgradeType.Attack,   5 },
                { UpgradeType.DashCD,   8 },
                { UpgradeType.SoulRate, 6 },
                { UpgradeType.MaxMana,  7 },
            };

        // ── Stat delta table ──────────────────────────────────────────────────
        private const float MaxHPDelta    =  20f;
        private const float AttackDelta   =   5f;
        private const float DashCDDelta   =  -0.13f;
        private const float SoulRateDelta =   3f;
        private const float MaxManaDelta  =  10f;

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Player Stat References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerMana   playerMana;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerMovement playerMovement;

        [Header("Events")]
        public UnityEvent<UpgradeType> onUpgradeApplied;
        public UnityEvent              onInsufficientShards;

        // ── Runtime bonuses ───────────────────────────────────────────────────
        [Header("Current Bonuses (read-only in editor)")]
        [SerializeField] private float _maxHPBonus;
        [SerializeField] private float _attackBonus;
        [SerializeField] private float _dashCDReduction;
        [SerializeField] private float _soulRateBonus;
        [SerializeField] private float _maxManaBonus;

        // ── Upgrade purchase counts ───────────────────────────────────────────
        private readonly Dictionary<UpgradeType, int> _purchaseCounts =
            new Dictionary<UpgradeType, int>
            {
                { UpgradeType.MaxHP,    0 },
                { UpgradeType.Attack,   0 },
                { UpgradeType.DashCD,   0 },
                { UpgradeType.SoulRate, 0 },
                { UpgradeType.MaxMana,  0 },
            };

        // ── Public accessors ──────────────────────────────────────────────────
        public float MaxHPBonus      => _maxHPBonus;
        public float AttackBonus     => _attackBonus;
        public float DashCDReduction => _dashCDReduction;
        public float SoulRateBonus   => _soulRateBonus;
        public float MaxManaBonus    => _maxManaBonus;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ResolvePlayerReferences();
            LoadFromSave();
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>
        /// Checks whether the player has enough shards to purchase the given upgrade.
        /// </summary>
        public bool CanAfford(UpgradeType type)
        {
            if (!CostTable.TryGetValue(type, out int cost)) return false;
            return GameManager.Instance != null && GameManager.Instance.TotalShards >= cost;
        }

        /// <summary>
        /// Deducts shards and applies the stat bonus for the given upgrade type.
        /// Does nothing and fires <see cref="onInsufficientShards"/> if shards are insufficient.
        /// </summary>
        public void ApplyUpgrade(UpgradeType type)
        {
            if (!CanAfford(type))
            {
                onInsufficientShards.Invoke();
                return;
            }

            int cost = CostTable[type];
            GameManager.Instance.TotalShards -= cost;
            _purchaseCounts[type]++;

            ApplyStatChange(type);
            onUpgradeApplied.Invoke(type);
            SaveSystem.Save();

            Debug.Log($"[UpgradeSystem] Purchased {type} (cost {cost}). Remaining shards: {GameManager.Instance.TotalShards}");
        }

        /// <summary>Returns the shard cost for the given upgrade type.</summary>
        public int GetCost(UpgradeType type) =>
            CostTable.TryGetValue(type, out int c) ? c : 0;

        /// <summary>Returns how many times the player has purchased a given upgrade.</summary>
        public int GetPurchaseCount(UpgradeType type) =>
            _purchaseCounts.TryGetValue(type, out int n) ? n : 0;

        // =========================================================================
        //  Private: stat application
        // =========================================================================

        private void ApplyStatChange(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MaxHP:
                    _maxHPBonus += MaxHPDelta;
                    if (playerHealth != null)
                        playerHealth.AddMaxHP(MaxHPDelta);
                    break;

                case UpgradeType.Attack:
                    _attackBonus += AttackDelta;
                    if (playerCombat != null)
                        playerCombat.AddAttackBonus(AttackDelta);
                    break;

                case UpgradeType.DashCD:
                    _dashCDReduction += Mathf.Abs(DashCDDelta);
                    if (playerMovement != null)
                        playerMovement.ReduceDashCooldown(Mathf.Abs(DashCDDelta));
                    break;

                case UpgradeType.SoulRate:
                    _soulRateBonus += SoulRateDelta;
                    // PlayerSoulSystem would apply this; deferred to that class
                    break;

                case UpgradeType.MaxMana:
                    _maxManaBonus += MaxManaDelta;
                    if (playerMana != null)
                        playerMana.AddMaxMana(MaxManaDelta);
                    break;
            }
        }

        // =========================================================================
        //  Save / Load
        // =========================================================================

        private void LoadFromSave()
        {
            SaveData data = SaveSystem.Load();
            if (data?.upgrades == null) return;

            foreach (var kvp in data.upgrades)
            {
                if (System.Enum.TryParse(kvp.Key, out UpgradeType type))
                {
                    int count = kvp.Value;
                    _purchaseCounts[type] = count;

                    // Re-apply all accumulated upgrades silently (no shard cost on load)
                    for (int i = 0; i < count; i++)
                        ApplyStatChangeSilent(type);
                }
            }
        }

        /// <summary>Apply stat without deducting shards or firing events (used during load).</summary>
        private void ApplyStatChangeSilent(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.MaxHP:
                    _maxHPBonus += MaxHPDelta;
                    if (playerHealth != null) playerHealth.AddMaxHP(MaxHPDelta);
                    break;
                case UpgradeType.Attack:
                    _attackBonus += AttackDelta;
                    if (playerCombat != null) playerCombat.AddAttackBonus(AttackDelta);
                    break;
                case UpgradeType.DashCD:
                    _dashCDReduction += Mathf.Abs(DashCDDelta);
                    if (playerMovement != null) playerMovement.ReduceDashCooldown(Mathf.Abs(DashCDDelta));
                    break;
                case UpgradeType.SoulRate:
                    _soulRateBonus += SoulRateDelta;
                    break;
                case UpgradeType.MaxMana:
                    _maxManaBonus += MaxManaDelta;
                    if (playerMana != null) playerMana.AddMaxMana(MaxManaDelta);
                    break;
            }
        }

        /// <summary>Serialize current upgrade counts into a dictionary for SaveData.</summary>
        public Dictionary<string, int> SerializeUpgrades()
        {
            var dict = new Dictionary<string, int>();
            foreach (var kvp in _purchaseCounts)
                dict[kvp.Key.ToString()] = kvp.Value;
            return dict;
        }

        private void ResolvePlayerReferences()
        {
            GameObject player = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (player == null) return;

            if (playerHealth   == null) playerHealth   = player.GetComponent<PlayerHealth>();
            if (playerMana     == null) playerMana     = player.GetComponent<PlayerMana>();
            if (playerCombat   == null) playerCombat   = player.GetComponent<PlayerCombat>();
            if (playerMovement == null) playerMovement = player.GetComponent<PlayerMovement>();
        }
    }
}
