using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LastEmberKnight
{
    // =========================================================================
    //  Food definitions
    // =========================================================================

    [System.Serializable]
    public struct FoodDefinition
    {
        public FoodType type;
        public int      hpRestore;
        public int      mpRestore;
        public Color    displayColor;
        public Color    glowColor;
    }

    // =========================================================================
    //  FoodSystem
    // =========================================================================

    /// <summary>
    /// Manages the player's FIFO food queue (max 5 items).
    /// Press F to consume the first item in the queue.
    /// </summary>
    public class FoodSystem : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static FoodSystem Instance { get; private set; }

        // ── Static food data table ────────────────────────────────────────────
        private static readonly FoodDefinition[] FoodTable = new FoodDefinition[]
        {
            new FoodDefinition
            {
                type        = FoodType.EmberBread,
                hpRestore   = 20,
                mpRestore   = 0,
                displayColor = new Color(0.85f, 0.55f, 0.20f),
                glowColor    = new Color(1.0f,  0.65f, 0.25f),
            },
            new FoodDefinition
            {
                type        = FoodType.AshStew,
                hpRestore   = 40,
                mpRestore   = 10,
                displayColor = new Color(0.50f, 0.35f, 0.25f),
                glowColor    = new Color(0.70f, 0.50f, 0.35f),
            },
            new FoodDefinition
            {
                type        = FoodType.ManaDraught,
                hpRestore   = 0,
                mpRestore   = 30,
                displayColor = new Color(0.20f, 0.40f, 0.90f),
                glowColor    = new Color(0.40f, 0.60f, 1.0f),
            },
            new FoodDefinition
            {
                type        = FoodType.PhoenixElixir,
                hpRestore   = 60,
                mpRestore   = 20,
                displayColor = new Color(1.0f,  0.35f, 0.05f),
                glowColor    = new Color(1.0f,  0.65f, 0.10f),
            },
            new FoodDefinition
            {
                type        = FoodType.BoneJerky,
                hpRestore   = 15,
                mpRestore   = 5,
                displayColor = new Color(0.70f, 0.58f, 0.40f),
                glowColor    = new Color(0.85f, 0.72f, 0.52f),
            },
            new FoodDefinition
            {
                type        = FoodType.EmberFruit,
                hpRestore   = 10,
                mpRestore   = 15,
                displayColor = new Color(0.90f, 0.20f, 0.40f),
                glowColor    = new Color(1.0f,  0.40f, 0.55f),
            },
        };

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Queue Settings")]
        [SerializeField] private int maxQueueSize = 5;

        [Header("Player References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerMana   playerMana;

        [Header("Events")]
        public UnityEvent<FoodType> onFoodConsumed;
        public UnityEvent<FoodType> onFoodPickedUp;
        public UnityEvent            onQueueFull;
        public UnityEvent            onQueueChanged;

        // ── Runtime ───────────────────────────────────────────────────────────
        private readonly Queue<FoodType> _queue = new Queue<FoodType>();
        private readonly HashSet<FoodType> _eatenTypes = new HashSet<FoodType>();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ResolvePlayerReferences();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
                ConsumeFirstItem();
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>
        /// Pick up a food item. Adds to the FIFO queue if not full.
        /// </summary>
        /// <returns>True if item was added; false if queue is full.</returns>
        public bool PickupFood(FoodType type)
        {
            if (_queue.Count >= maxQueueSize)
            {
                onQueueFull.Invoke();
                return false;
            }

            _queue.Enqueue(type);
            onFoodPickedUp.Invoke(type);
            onQueueChanged.Invoke();
            return true;
        }

        /// <summary>Consume the first item in the queue immediately.</summary>
        public void ConsumeFirstItem()
        {
            if (_queue.Count == 0) return;

            FoodType type = _queue.Dequeue();
            ApplyFoodEffect(type);

            _eatenTypes.Add(type);
            onFoodConsumed.Invoke(type);
            onQueueChanged.Invoke();

            // Achievement: Gourmet (ate all 6 food types)
            if (_eatenTypes.Count >= System.Enum.GetValues(typeof(FoodType)).Length)
                AchievementTracker.Instance?.CheckAchievement("Gourmet");
        }

        /// <summary>Returns the current queue contents as a read-only list (front to back).</summary>
        public FoodType[] GetQueue() => _queue.ToArray();

        /// <summary>Returns the number of items currently in the queue.</summary>
        public int QueueCount => _queue.Count;

        /// <summary>Returns whether the queue is full.</summary>
        public bool IsFull => _queue.Count >= maxQueueSize;

        /// <summary>Returns the static definition for a given food type.</summary>
        public static FoodDefinition GetDefinition(FoodType type)
        {
            foreach (FoodDefinition def in FoodTable)
                if (def.type == type) return def;
            return default;
        }

        // =========================================================================
        //  Private helpers
        // =========================================================================

        private void ApplyFoodEffect(FoodType type)
        {
            FoodDefinition def = GetDefinition(type);

            if (def.hpRestore > 0 && playerHealth != null)
                playerHealth.Heal(def.hpRestore);

            if (def.mpRestore > 0 && playerMana != null)
                playerMana.Restore(def.mpRestore);

            AudioManager.Instance?.PlaySound(SoundType.ShardPickup, 0.8f, 1.2f);

            Debug.Log($"[FoodSystem] Consumed {type}: +{def.hpRestore} HP, +{def.mpRestore} MP");
        }

        private void ResolvePlayerReferences()
        {
            if (playerHealth != null && playerMana != null) return;

            GameObject player = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
            if (player == null) return;

            if (playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
            if (playerMana   == null) playerMana   = player.GetComponent<PlayerMana>();
        }
    }
}
