using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Manages which skills the player has unlocked.
    /// Skills are tied to <see cref="AbilityType"/> and unlock at fixed level thresholds.
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static SkillSystem Instance { get; private set; }

        // ── Unlock thresholds ─────────────────────────────────────────────────
        private static readonly (AbilityType ability, int unlockLevel)[] UnlockTable =
        {
            (AbilityType.EmberWave,  0),
            (AbilityType.FireShield, 8),
            (AbilityType.Inferno,   18),
        };

        // ── Runtime state ─────────────────────────────────────────────────────
        private readonly HashSet<AbilityType> _unlockedSkills = new HashSet<AbilityType>();

        /// <summary>Raised whenever a new skill is unlocked. Passes the newly unlocked ability.</summary>
        public static event Action<AbilityType> OnSkillUnlocked;

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Player Abilities Reference")]
        [SerializeField] private PlayerAbilities playerAbilities;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Resolve PlayerAbilities from the scene if not assigned
            if (playerAbilities == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
                if (player != null)
                    playerAbilities = player.GetComponent<PlayerAbilities>();
            }

            // Restore any previously saved unlocks
            LoadFromSave();
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>
        /// Evaluates all unlock thresholds and activates any skills the player
        /// now qualifies for based on <paramref name="level"/>.
        /// </summary>
        public void CheckUnlocks(int level)
        {
            foreach (var (ability, unlockLevel) in UnlockTable)
            {
                if (level >= unlockLevel && !_unlockedSkills.Contains(ability))
                    UnlockSkill(ability);
            }
        }

        /// <summary>Returns true if the given skill has been unlocked.</summary>
        public bool IsUnlocked(AbilityType ability) => _unlockedSkills.Contains(ability);

        /// <summary>Unlock a specific skill directly (used during save-load restoration).</summary>
        public void UnlockSkill(AbilityType ability)
        {
            if (_unlockedSkills.Add(ability))
            {
                // Notify PlayerAbilities
                if (playerAbilities != null)
                    playerAbilities.UnlockAbility(ability);

                OnSkillUnlocked?.Invoke(ability);

                Debug.Log($"[SkillSystem] Unlocked: {ability}");
            }
        }

        /// <summary>Returns all currently unlocked abilities as a read-only set.</summary>
        public IReadOnlyCollection<AbilityType> GetUnlockedSkills() => _unlockedSkills;

        // =========================================================================
        //  Save / Load integration
        // =========================================================================

        private void LoadFromSave()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return;

            if (data.unlockedAbilities == null) return;

            foreach (AbilityType ab in data.unlockedAbilities)
            {
                if (_unlockedSkills.Add(ab) && playerAbilities != null)
                    playerAbilities.UnlockAbility(ab);
            }
        }
    }
}
