using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    // =========================================================================
    //  Achievement data
    // =========================================================================

    [System.Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public int    iconIndex;
    }

    // =========================================================================
    //  AchievementTracker
    // =========================================================================

    /// <summary>
    /// Tracks all 12 achievements, stores earned state via SaveSystem,
    /// and fires <see cref="OnAchievementUnlocked"/> to drive the popup UI.
    /// </summary>
    public class AchievementTracker : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static AchievementTracker Instance { get; private set; }

        // ── Achievement registry ──────────────────────────────────────────────
        private static readonly AchievementDefinition[] AchievementTable = new AchievementDefinition[]
        {
            new AchievementDefinition
            {
                id = "FirstBlood", displayName = "First Blood",
                description = "Kill your first enemy.", iconIndex = 0
            },
            new AchievementDefinition
            {
                id = "Halfway", displayName = "Halfway There",
                description = "Reach level 25.", iconIndex = 1
            },
            new AchievementDefinition
            {
                id = "Legend", displayName = "Legend",
                description = "Complete all 50 levels.", iconIndex = 2
            },
            new AchievementDefinition
            {
                id = "WardenSlain", displayName = "Warden Slain",
                description = "Defeat the Ashen Warden (Boss 0).", iconIndex = 3
            },
            new AchievementDefinition
            {
                id = "LordOfAsh", displayName = "Lord of Ash",
                description = "Defeat Drakar, Lord of Ash (Boss 9).", iconIndex = 4
            },
            new AchievementDefinition
            {
                id = "Untouchable", displayName = "Untouchable",
                description = "Complete a level without taking any damage.", iconIndex = 5
            },
            new AchievementDefinition
            {
                id = "ShardHoarder", displayName = "Shard Hoarder",
                description = "Accumulate 100 shards.", iconIndex = 6
            },
            new AchievementDefinition
            {
                id = "Gourmet", displayName = "Gourmet",
                description = "Eat all 6 types of food.", iconIndex = 7
            },
            new AchievementDefinition
            {
                id = "DashMaster", displayName = "Dash Master",
                description = "Dash 100 times.", iconIndex = 8
            },
            new AchievementDefinition
            {
                id = "EmberMage", displayName = "Ember Mage",
                description = "Use special skills 50 times.", iconIndex = 9
            },
            new AchievementDefinition
            {
                id = "PerfectParry", displayName = "Perfect Parry",
                description = "Successfully parry an attack.", iconIndex = 10
            },
            new AchievementDefinition
            {
                id = "FromAbove", displayName = "From Above",
                description = "Kill an enemy with the Slam.", iconIndex = 11
            },
        };

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private AchievementPopup achievementPopup;

        // ── Runtime counters ──────────────────────────────────────────────────
        private int _dashCount      = 0;
        private int _skillUseCount  = 0;

        // ── Earned set ────────────────────────────────────────────────────────
        private readonly HashSet<string> _earned = new HashSet<string>();

        /// <summary>Fired when any achievement is unlocked. Carries the achievement id.</summary>
        public static event Action<string> OnAchievementUnlocked;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Resolve popup reference if not set
            if (achievementPopup == null)
                achievementPopup = FindObjectOfType<AchievementPopup>();

            LoadFromSave();
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>
        /// Checks whether an achievement with <paramref name="id"/> should be awarded.
        /// If not already earned, earns it and fires the unlock event.
        /// </summary>
        public void CheckAchievement(string id)
        {
            if (_earned.Contains(id)) return;

            AchievementDefinition def = FindDefinition(id);
            if (def == null)
            {
                Debug.LogWarning($"[AchievementTracker] Unknown achievement id: {id}");
                return;
            }

            EarnAchievement(def);
        }

        // ── Per-event helpers (called by gameplay systems) ─────────────────────

        public void NotifyEnemyKilled()
        {
            CheckAchievement("FirstBlood");
        }

        public void NotifyLevelReached(int level)
        {
            if (level >= 24) CheckAchievement("Halfway");
            if (level >= 49) CheckAchievement("Legend");
        }

        public void NotifyBossDefeated(int bossIndex)
        {
            if (bossIndex == 0) CheckAchievement("WardenSlain");
            if (bossIndex == 9) CheckAchievement("LordOfAsh");
        }

        public void NotifyShardsAccumulated(int total)
        {
            if (total >= 100) CheckAchievement("ShardHoarder");
        }

        public void NotifyDash()
        {
            _dashCount++;
            if (_dashCount >= 100) CheckAchievement("DashMaster");
        }

        public void NotifySkillUsed()
        {
            _skillUseCount++;
            if (_skillUseCount >= 50) CheckAchievement("EmberMage");
        }

        public void NotifyParry()           => CheckAchievement("PerfectParry");
        public void NotifySlamKill()        => CheckAchievement("FromAbove");
        public void NotifyGourmet()         => CheckAchievement("Gourmet");
        public void NotifyUntouchable()     => CheckAchievement("Untouchable");

        /// <summary>Returns true if the achievement with the given id has been earned.</summary>
        public bool IsEarned(string id) => _earned.Contains(id);

        /// <summary>Returns all achievement definitions (for UI listing).</summary>
        public static IReadOnlyList<AchievementDefinition> GetAllDefinitions()
            => AchievementTable;

        // =========================================================================
        //  Private helpers
        // =========================================================================

        private void EarnAchievement(AchievementDefinition def)
        {
            _earned.Add(def.id);

            // Show popup
            if (achievementPopup != null)
                achievementPopup.ShowAchievement(def.displayName, def.description, def.iconIndex);

            OnAchievementUnlocked?.Invoke(def.id);

            SaveSystem.Save();

            Debug.Log($"[AchievementTracker] Unlocked: {def.displayName}");
        }

        private static AchievementDefinition FindDefinition(string id)
        {
            foreach (AchievementDefinition def in AchievementTable)
                if (def.id == id) return def;
            return null;
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        private void LoadFromSave()
        {
            SaveData data = SaveSystem.Load();
            if (data?.achievements == null) return;

            foreach (string id in data.achievements)
                _earned.Add(id);
        }
    }
}
