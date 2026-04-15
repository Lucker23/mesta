using System;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Singleton that manages shard (currency) spawning and collection.
    /// EnemyBase calls ShardManager.Instance.SpawnShards() on enemy death.
    /// </summary>
    public class ShardManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static ShardManager Instance { get; private set; }

        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Shard Prefab")]
        [SerializeField] private GameObject shardPrefab;

        [Header("Spawn Physics")]
        [SerializeField] private float minSpawnForceX  = -2f;
        [SerializeField] private float maxSpawnForceX  =  2f;
        [SerializeField] private float minSpawnForceY  =  2f;
        [SerializeField] private float maxSpawnForceY  =  5f;

        // ── Events ─────────────────────────────────────────────────────────────
        public static event Action<int> OnShardsSpawned;
        public static event Action<int> OnShardCollected;   // total collected

        // ── Runtime ────────────────────────────────────────────────────────────
        private int totalCollected;
        public int TotalCollected => totalCollected;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Spawn count shard pickups at the given world position.</summary>
        public void SpawnShards(Vector3 worldPosition, int count)
        {
            if (shardPrefab == null)
            {
                // Fallback: just award shards directly without a visible pickup
                AwardShards(count);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.2f, 0.2f),
                    UnityEngine.Random.Range(0f, 0.3f),
                    0f);

                GameObject shard = Instantiate(shardPrefab, worldPosition + offset, Quaternion.identity);

                Rigidbody2D rb = shard.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(new Vector2(
                        UnityEngine.Random.Range(minSpawnForceX, maxSpawnForceX),
                        UnityEngine.Random.Range(minSpawnForceY, maxSpawnForceY)),
                        ForceMode2D.Impulse);
                }
            }

            OnShardsSpawned?.Invoke(count);
        }

        /// <summary>Called by a shard pickup when the player collects it.</summary>
        public void AwardShards(int count)
        {
            totalCollected += count;

            if (GameManager.Instance != null)
                GameManager.Instance.TotalShards += count;

            OnShardCollected?.Invoke(totalCollected);
        }
    }
}
