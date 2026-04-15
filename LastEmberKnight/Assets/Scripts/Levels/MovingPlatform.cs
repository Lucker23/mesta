using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Simple oscillating platform. Moves between two points along the X or Y axis.
    /// Set movement range and speed via <see cref="SetMovementRange"/>.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float range  = 2f;   // half-range from start position
        [SerializeField] private float speed  = 2f;   // units per second
        [SerializeField] private bool  vertical = false;

        private Vector3 _origin;
        private float   _phase;

        private void Awake()
        {
            _origin = transform.position;
            // Random phase so not all platforms sync
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void FixedUpdate()
        {
            _phase += Time.fixedDeltaTime * speed;
            float offset = Mathf.Sin(_phase) * range;

            Vector3 newPos = _origin;
            if (vertical) newPos.y += offset;
            else          newPos.x += offset;

            // Move Rigidbody2D if present (so it pushes players)
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.MovePosition(newPos);
            else
                transform.position = newPos;
        }

        /// <summary>Configure movement range (half-extent) and speed.</summary>
        public void SetMovementRange(float halfRange, float moveSpeed)
        {
            range = halfRange;
            speed = moveSpeed;
            _origin = transform.position;   // re-anchor to current position
        }
    }
}
