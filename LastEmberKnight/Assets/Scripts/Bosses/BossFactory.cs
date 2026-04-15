using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Spawns boss GameObjects by index and manages the Lucker23 easter-egg swap.
    /// Boss indices match BossType enum:
    ///   0=AshenWarden  1=ElderTitan  2=DragonAspect  3=BoneKing    4=TheFlood
    ///   5=SandWraith   6=ObsidianSentinel  7=TheCorruption  8=StormDrake  9=DarkLordDrakar
    /// The ElderTitan (index 1) is the Lucker23 easter egg boss.
    /// </summary>
    public class BossFactory : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────

        public static BossFactory Instance { get; private set; }

        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Boss Prefabs (index matches BossType enum)")]
        [SerializeField] private GameObject[] bossPrefabs = new GameObject[10];

        [Header("Lucker23 Easter Egg")]
        [SerializeField] private GameObject elderTitanPrefab;

        // ── Internal ───────────────────────────────────────────────────────

        // Maps bossIndex → active BossBase instance (only one per index at a time)
        private readonly Dictionary<int, BossBase> _activeBosses = new Dictionary<int, BossBase>();

        // ── Unity Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Instantiates and returns the BossBase for the given index at the specified world position.
        /// Returns null if the index is out of range or the prefab is not assigned.
        /// </summary>
        public BossBase SpawnBoss(int bossIndex, Vector3 position)
        {
            if (bossIndex < 0 || bossIndex >= bossPrefabs.Length)
            {
                Debug.LogWarning($"[BossFactory] SpawnBoss: bossIndex {bossIndex} is out of range.");
                return null;
            }

            GameObject prefab = bossPrefabs[bossIndex];
            if (prefab == null)
            {
                Debug.LogWarning($"[BossFactory] SpawnBoss: prefab for boss index {bossIndex} is not assigned.");
                return null;
            }

            GameObject bossGO = Instantiate(prefab, position, Quaternion.identity);
            BossBase boss = bossGO.GetComponent<BossBase>();

            if (boss == null)
            {
                Debug.LogError($"[BossFactory] Spawned prefab for index {bossIndex} has no BossBase component.");
                Destroy(bossGO);
                return null;
            }

            // Track the active boss so we can reference it later
            if (_activeBosses.ContainsKey(bossIndex))
                _activeBosses[bossIndex] = boss;
            else
                _activeBosses.Add(bossIndex, boss);

            Debug.Log($"[BossFactory] Spawned boss '{boss.GetDisplayName()}' (index {bossIndex}) at {position}.");
            return boss;
        }

        /// <summary>
        /// Lucker23 easter-egg hack: destroys the current boss and replaces it with
        /// the ElderTitan at the same position.
        /// Typically called when the player reaches Level 10 (boss index 1 zone).
        /// </summary>
        public BossBase ApplyElderTitanSwap(BossBase currentBoss)
        {
            if (currentBoss == null)
            {
                Debug.LogWarning("[BossFactory] ApplyElderTitanSwap: currentBoss is null.");
                return null;
            }

            if (elderTitanPrefab == null)
            {
                Debug.LogWarning("[BossFactory] ApplyElderTitanSwap: elderTitanPrefab is not assigned.");
                return null;
            }

            Vector3 spawnPos = currentBoss.transform.position;
            int originalIndex = currentBoss.BossIndex;

            // Remove and destroy the current boss silently (no death events)
            if (_activeBosses.ContainsKey(originalIndex))
                _activeBosses.Remove(originalIndex);

            Destroy(currentBoss.gameObject);

            // Spawn the ElderTitan in its place
            GameObject elderGO = Instantiate(elderTitanPrefab, spawnPos, Quaternion.identity);
            BossBase elderBoss = elderGO.GetComponent<BossBase>();

            if (elderBoss == null)
            {
                Debug.LogError("[BossFactory] ElderTitan prefab has no BossBase component!");
                Destroy(elderGO);
                return null;
            }

            _activeBosses[originalIndex] = elderBoss;

            Debug.Log("[BossFactory] Lucker23 easter egg activated! ElderTitan has appeared.");
            return elderBoss;
        }

        /// <summary>Returns the currently active boss for the given index, or null.</summary>
        public BossBase GetActiveBoss(int bossIndex)
        {
            _activeBosses.TryGetValue(bossIndex, out BossBase boss);
            return boss;
        }

        /// <summary>Removes the boss entry when a boss is destroyed externally.</summary>
        public void UnregisterBoss(int bossIndex)
        {
            if (_activeBosses.ContainsKey(bossIndex))
                _activeBosses.Remove(bossIndex);
        }
    }
}
