using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Procedurally generates each level: platforms, enemies, shrines, shards,
    /// food items, and the exit door. Boss levels produce a flat arena instead.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        // ── Singleton / static prepare hook (called by GameManager before scene load) ──
        public static LevelGenerator Instance { get; private set; }

        // Pre-level prep data stored statically so it survives scene transitions
        private static int    s_pendingLevel   = 0;
        private static ZoneType s_pendingZone  = ZoneType.Ashfields;
        private static bool   s_pendingIsBoss  = false;

        /// <summary>Called by GameManager before the scene loads to cache level parameters.</summary>
        public static void PrepareLevel(int level, ZoneType zone, bool isBoss)
        {
            s_pendingLevel  = level;
            s_pendingZone   = zone;
            s_pendingIsBoss = isBoss;
        }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private PlatformBuilder platformBuilder;
        [SerializeField] private EnemyFactory    enemyFactory;

        [Header("Prefabs")]
        [SerializeField] private GameObject shrinePrefab;
        [SerializeField] private GameObject exitDoorPrefab;
        [SerializeField] private GameObject shardPrefab;
        [SerializeField] private GameObject foodItemPrefab;

        [Header("Settings")]
        [SerializeField] private float groundY    = 0f;
        [SerializeField] private float levelHeight = 15f;

        // ── Constants ─────────────────────────────────────────────────────────
        private const float BossArenaWidth   = 40f;
        private const int   BossPlatformCount = 4;

        // ── Runtime ───────────────────────────────────────────────────────────
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private ZoneType currentZone;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GenerateLevel(s_pendingLevel);
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>
        /// Generate a level from scratch. Destroys any previously spawned objects first.
        /// Normal levels: width = 30 + levelIndex * 2, height fixed at 15 units.
        /// Boss levels (every 5th in a zone): flat 40-unit arena with 4 elevated platforms.
        /// </summary>
        public void GenerateLevel(int levelIndex)
        {
            DestroyCurrentLevel();

            currentZone = ZoneManager.GetZone(levelIndex);
            bool isBoss = GameManager.Instance != null
                ? GameManager.Instance.IsBossLevel(levelIndex)
                : s_pendingIsBoss;

            if (isBoss)
                GenerateBossLevel(levelIndex);
            else
                GenerateNormalLevel(levelIndex);

            // Build zone background after geometry is placed
            if (ZoneManager.Instance != null)
                ZoneManager.Instance.BuildZoneBackground(currentZone);
        }

        /// <summary>Destroy all GameObjects spawned by the last GenerateLevel call.</summary>
        public void DestroyCurrentLevel()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            spawnedObjects.Clear();
        }

        // =========================================================================
        //  Normal level
        // =========================================================================

        private void GenerateNormalLevel(int levelIndex)
        {
            float width  = 30f + levelIndex * 2f;

            // --- Ground --------------------------------------------------------
            SpawnPlatform(new Vector2(width * 0.5f, groundY - 0.5f),
                          new Vector2(width + 4f, 1f), false, false);

            // --- Procedural platforms ------------------------------------------
            float gapMin    = 1.5f;
            float gapMax    = 3.5f;
            float heightMin = 2f;
            float heightMax = 6f;
            System.Random rng = new System.Random(levelIndex * 1337);
            float cursor = 4f;

            var platformPositions = new List<Vector2>();

            while (cursor < width - 6f)
            {
                float pWidth  = (float)(rng.NextDouble() * 3.0 + 3.0);
                float pHeight = (float)(rng.NextDouble() * (heightMax - heightMin) + heightMin);
                Vector2 pos  = new Vector2(cursor + pWidth * 0.5f, groundY + pHeight);
                Vector2 size = new Vector2(pWidth, 0.5f);

                bool moving = rng.NextDouble() < 0.15;
                bool oneWay = rng.NextDouble() < 0.20;

                SpawnPlatform(pos, size, moving, oneWay);
                platformPositions.Add(pos);

                float gap = (float)(rng.NextDouble() * (gapMax - gapMin) + gapMin);
                cursor += pWidth + gap;
            }

            // --- Enemies -------------------------------------------------------
            int difficulty     = Mathf.Clamp(levelIndex / 5, 1, 10);
            int enemyCount     = Mathf.Min(3 + levelIndex / 3, 15);

            if (enemyFactory != null)
            {
                for (int i = 0; i < enemyCount; i++)
                {
                    float ex = (float)(rng.NextDouble() * (width - 8f) + 4f);
                    Vector2 ePos = new Vector2(ex, groundY + 1f);
                    // Skip shrine area (midpoint ± 2 units)
                    if (Mathf.Abs(ex - width * 0.5f) < 2f) continue;

                    GameObject enemy = enemyFactory.SpawnEnemy(ePos, difficulty, currentZone);
                    if (enemy != null) spawnedObjects.Add(enemy);
                }
            }

            // --- Shrine at midpoint -------------------------------------------
            float midX = width * 0.5f;
            SpawnPrefab(shrinePrefab, new Vector2(midX, groundY + 1f));

            // --- Exit door at end ---------------------------------------------
            SpawnPrefab(exitDoorPrefab, new Vector2(width - 2f, groundY + 1f));

            // --- Shards --------------------------------------------------------
            int shardCount = 5 + levelIndex / 2;
            for (int i = 0; i < shardCount; i++)
            {
                float sx = (float)(rng.NextDouble() * (width - 4f) + 2f);
                float sy = groundY + (float)(rng.NextDouble() * 4f + 1f);
                SpawnPrefab(shardPrefab, new Vector2(sx, sy));
            }

            // --- Food on platforms --------------------------------------------
            int foodCount = Mathf.Clamp(2 + levelIndex / 8, 2, 5);
            FoodType[] foodTypes = (FoodType[])System.Enum.GetValues(typeof(FoodType));

            for (int i = 0; i < foodCount; i++)
            {
                Vector2 platPos = platformPositions.Count > 0
                    ? platformPositions[rng.Next(platformPositions.Count)]
                    : new Vector2((float)(rng.NextDouble() * width), groundY + 2f);

                if (foodItemPrefab != null)
                {
                    GameObject food = Instantiate(foodItemPrefab,
                        new Vector3(platPos.x, platPos.y + 1f, 0f), Quaternion.identity);
                    FoodPickup pickup = food.GetComponent<FoodPickup>();
                    if (pickup != null)
                        pickup.SetFoodType(foodTypes[rng.Next(foodTypes.Length)]);
                    spawnedObjects.Add(food);
                }
            }
        }

        // =========================================================================
        //  Boss level
        // =========================================================================

        private void GenerateBossLevel(int levelIndex)
        {
            int bossIndex = levelIndex / GameConstants.LevelsPerZone;   // 0-9

            // --- Flat ground --------------------------------------------------
            SpawnPlatform(new Vector2(BossArenaWidth * 0.5f, groundY - 0.5f),
                          new Vector2(BossArenaWidth + 4f, 1f), false, false);

            // --- 4 elevated platforms ----------------------------------------
            float[] platformHeights = { 3f, 5f, 3f, 5f };
            for (int i = 0; i < BossPlatformCount; i++)
            {
                float px = 5f + i * (BossArenaWidth / (BossPlatformCount + 1f));
                float py = groundY + platformHeights[i % platformHeights.Length];
                SpawnPlatform(new Vector2(px, py), new Vector2(6f, 0.5f), false, false);
            }

            // --- Boss ---------------------------------------------------------
            if (enemyFactory != null)
            {
                GameObject boss = enemyFactory.SpawnBoss(
                    new Vector2(BossArenaWidth * 0.5f, groundY + 2f), bossIndex, currentZone);
                if (boss != null) spawnedObjects.Add(boss);
            }

            // --- Exit door (boss mode) ----------------------------------------
            if (exitDoorPrefab != null)
            {
                GameObject exit = Instantiate(exitDoorPrefab,
                    new Vector3(BossArenaWidth - 2f, groundY + 1f, 0f), Quaternion.identity);
                ExitDoor door = exit.GetComponent<ExitDoor>();
                if (door != null) door.SetBossMode(true);
                spawnedObjects.Add(exit);
            }
        }

        // =========================================================================
        //  Helpers
        // =========================================================================

        private void SpawnPlatform(Vector2 pos, Vector2 size, bool moving, bool oneWay)
        {
            if (platformBuilder == null) return;
            GameObject plat = platformBuilder.CreatePlatform(pos, size, currentZone, moving, oneWay);
            if (plat != null) spawnedObjects.Add(plat);
        }

        private void SpawnPrefab(GameObject prefab, Vector2 pos)
        {
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            spawnedObjects.Add(go);
        }
    }
}
