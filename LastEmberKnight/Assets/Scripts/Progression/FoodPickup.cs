using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// World-space pickup that adds a food item to the player's <see cref="FoodSystem"/> queue
    /// on contact and then destroys itself. Visual colour is derived from the food type.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class FoodPickup : MonoBehaviour
    {
        [SerializeField] private FoodType foodType = FoodType.EmberBread;

        private SpriteRenderer _sr;
        private float _bobTime;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius    = 0.35f;

            ApplyVisual();
        }

        private void Update()
        {
            // Gentle bob animation
            _bobTime += Time.deltaTime * 2f;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                transform.localPosition.y + Mathf.Sin(_bobTime) * 0.002f,
                transform.localPosition.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(GameConstants.TagPlayer)) return;

            if (FoodSystem.Instance != null && FoodSystem.Instance.PickupFood(foodType))
            {
                AudioManager.Instance?.PlaySound(SoundType.ShardPickup, 0.6f, 0.9f);
                Destroy(gameObject);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Set the food type this pickup represents (called by LevelGenerator).</summary>
        public void SetFoodType(FoodType type)
        {
            foodType = type;
            ApplyVisual();
        }

        // ── Visual ────────────────────────────────────────────────────────────

        private void ApplyVisual()
        {
            if (_sr == null) return;

            FoodDefinition def = FoodSystem.GetDefinition(foodType);
            _sr.color = def.displayColor;

            // Simple glow via a second sprite child
            Light foodLight = GetComponentInChildren<Light>();
            if (foodLight != null)
                foodLight.color = def.glowColor;
        }
    }
}
