using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastEmberKnight
{
    // =========================================================================
    //  SaveData
    // =========================================================================

    /// <summary>
    /// All persistent game data, serialised to JSON.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        // ── Progress ──────────────────────────────────────────────────────────
        public int   level;
        public int   totalShards;
        public float difficulty;

        // ── Upgrades: key = UpgradeType.ToString(), value = purchase count ────
        public SerializableDictionary upgrades = new SerializableDictionary();

        // ── Unlocked abilities ────────────────────────────────────────────────
        public List<AbilityType> unlockedAbilities = new List<AbilityType>();

        // ── Achievements (set of earned ids) ─────────────────────────────────
        public List<string> achievements = new List<string>();

        // ── Difficulty setting (0 = Normal, 1 = Hard, 2 = Inferno) ───────────
        public int difficultyPreset;

        // ── Best completion times per level (seconds, 0 = not completed) ─────
        public float[] bestTimes = new float[GameConstants.TotalLevels];

        // ── Timestamp ────────────────────────────────────────────────────────
        public string saveTimestamp;
    }

    // =========================================================================
    //  SerializableDictionary (string → int, Unity JSON-compatible)
    // =========================================================================

    [System.Serializable]
    public class SerializableDictionary : ISerializationCallbackReceiver
    {
        [SerializeField] private List<string> _keys   = new List<string>();
        [SerializeField] private List<int>    _values = new List<int>();

        private Dictionary<string, int> _dict = new Dictionary<string, int>();

        public int Count => _dict.Count;

        public bool TryGetValue(string key, out int value) =>
            _dict.TryGetValue(key, out value);

        public void Set(string key, int value) => _dict[key] = value;
        public int  Get(string key) => _dict.TryGetValue(key, out int v) ? v : 0;

        public IEnumerable<KeyValuePair<string, int>> GetAll() => _dict;

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kvp in _dict) { _keys.Add(kvp.Key); _values.Add(kvp.Value); }
        }

        public void OnAfterDeserialize()
        {
            _dict = new Dictionary<string, int>();
            int count = Mathf.Min(_keys.Count, _values.Count);
            for (int i = 0; i < count; i++)
                _dict[_keys[i]] = _values[i];
        }
    }

    // =========================================================================
    //  SaveSystem – static utility class
    // =========================================================================

    /// <summary>
    /// Static utility for JSON serialisation of <see cref="SaveData"/> to disk.
    /// Save file lives at <c>Application.persistentDataPath/save.json</c>.
    /// </summary>
    public static class SaveSystem
    {
        private const string SaveFileName = "save.json";

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Serialise the current runtime state (gathered from live singletons)
        /// and write it to disk.
        /// </summary>
        public static void Save()
        {
            SaveData data = BuildFromRuntime();
            WriteToFile(data);
            Debug.Log($"[SaveSystem] Saved to {SavePath}");
        }

        /// <summary>
        /// Save an explicit <see cref="SaveData"/> object (used by GameManager).
        /// </summary>
        public static void SaveGame(SaveData data)
        {
            if (data == null) return;
            data.saveTimestamp = DateTime.UtcNow.ToString("o");
            WriteToFile(data);
        }

        /// <summary>
        /// Deserialise save.json and return the <see cref="SaveData"/>.
        /// Returns null if no save file exists or if parsing fails.
        /// </summary>
        public static SaveData Load()
        {
            if (!File.Exists(SavePath)) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] Loaded from {SavePath} (level {data?.level})");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load save: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete the save file.
        /// </summary>
        public static void Delete()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save deleted.");
            }
        }

        /// <summary>
        /// Returns true if a save file exists on disk.
        /// </summary>
        public static bool HasSave() => File.Exists(SavePath);

        // ── Private helpers ───────────────────────────────────────────────────

        private static SaveData BuildFromRuntime()
        {
            SaveData data = new SaveData();
            data.saveTimestamp = DateTime.UtcNow.ToString("o");

            // Core progress from GameManager
            if (GameManager.Instance != null)
            {
                data.level       = GameManager.Instance.Level;
                data.totalShards = GameManager.Instance.TotalShards;
                data.difficulty  = GameManager.Instance.Difficulty;
            }

            // Upgrades
            if (UpgradeSystem.Instance != null)
            {
                foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
                {
                    int count = UpgradeSystem.Instance.GetPurchaseCount(type);
                    if (count > 0)
                        data.upgrades.Set(type.ToString(), count);
                }
            }

            // Unlocked abilities
            if (SkillSystem.Instance != null)
            {
                foreach (AbilityType ab in SkillSystem.Instance.GetUnlockedSkills())
                    data.unlockedAbilities.Add(ab);
            }

            // Achievements
            if (AchievementTracker.Instance != null)
            {
                foreach (AchievementDefinition def in AchievementTracker.GetAllDefinitions())
                {
                    if (AchievementTracker.Instance.IsEarned(def.id))
                        data.achievements.Add(def.id);
                }
            }

            return data;
        }

        private static void WriteToFile(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to write save: {ex.Message}");
            }
        }
    }
}
