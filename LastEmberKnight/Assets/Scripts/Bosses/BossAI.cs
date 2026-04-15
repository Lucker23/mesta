using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Shared AI logic component for all bosses.
    /// Attach alongside BossBase-derived components to provide movement,
    /// telegraph, and arena-awareness utilities.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossAI : MonoBehaviour
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Arena")]
        [SerializeField] private float arenaWidth = 20f;
        [SerializeField] private Vector2 arenaCenter = Vector2.zero;

        [Header("Attack Timing - Phase 1")]
        [SerializeField] private float phase1AttackInterval = 2.5f;

        [Header("Attack Timing - Phase 2")]
        [SerializeField] private float phase2AttackInterval = 1.5f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 14f;
        [SerializeField] private float jumpAirTime = 0.5f;

        [Header("Telegraph")]
        [SerializeField] private GameObject telegraphIndicatorPrefab;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayerMask;

        // ── Runtime State ──────────────────────────────────────────────────

        private Rigidbody2D _rb;
        private SpriteRenderer _sr;
        private bool _isJumping;

        // ── Properties ─────────────────────────────────────────────────────

        /// <summary>Width of the boss arena in world units.</summary>
        public float ArenaWidth => arenaWidth;

        /// <summary>Center of the boss arena in world space.</summary>
        public Vector2 ArenaCenter => arenaCenter;

        /// <summary>Left boundary of the arena.</summary>
        public float ArenaLeft => arenaCenter.x - arenaWidth * 0.5f;

        /// <summary>Right boundary of the arena.</summary>
        public float ArenaRight => arenaCenter.x + arenaWidth * 0.5f;

        // ── Unity Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sr = GetComponent<SpriteRenderer>();
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the recommended attack interval for the given phase (1 or 2).
        /// Phase 2 is significantly faster.
        /// </summary>
        public float GetPhaseAttackInterval(int phase)
        {
            return phase == 2 ? phase2AttackInterval : phase1AttackInterval;
        }

        /// <summary>
        /// Moves the boss horizontally toward the player at the given speed.
        /// Flips the sprite to face direction of travel.
        /// </summary>
        public void MoveTowardPlayer(float speed)
        {
            if (_rb == null) return;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            float dirX = playerObj.transform.position.x - transform.position.x;
            float moveDir = Mathf.Sign(dirX);

            _rb.velocity = new Vector2(moveDir * speed, _rb.velocity.y);

            if (_sr != null)
                _sr.flipX = moveDir < 0f;
        }

        /// <summary>
        /// Launches the boss toward <paramref name="target"/> with an arc jump.
        /// The boss becomes briefly airborne then lands at the target X position.
        /// </summary>
        public void JumpToPosition(Vector2 target)
        {
            if (_isJumping || _rb == null) return;
            StartCoroutine(JumpCoroutine(target));
        }

        /// <summary>
        /// Spawns a visual warning indicator at <paramref name="position"/> and holds it
        /// for <paramref name="duration"/> seconds before destroying it.
        /// </summary>
        public IEnumerator TelegraphAttack(float duration, Vector3 position)
        {
            GameObject indicator = null;
            if (telegraphIndicatorPrefab != null)
            {
                indicator = Instantiate(telegraphIndicatorPrefab, position, Quaternion.identity);
                SpriteRenderer indicatorSr = indicator.GetComponent<SpriteRenderer>();
                if (indicatorSr != null)
                {
                    indicatorSr.color = telegraphColor;
                }
            }

            // Pulse the indicator while waiting
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (indicator != null)
                {
                    float pulse = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 4f / duration));
                    SpriteRenderer indicatorSr = indicator.GetComponent<SpriteRenderer>();
                    if (indicatorSr != null)
                    {
                        Color c = telegraphColor;
                        c.a = pulse * telegraphColor.a;
                        indicatorSr.color = c;
                    }
                }

                yield return null;
            }

            if (indicator != null)
                Destroy(indicator);
        }

        /// <summary>Returns true when the boss is on the ground.</summary>
        public bool IsGrounded()
        {
            if (groundCheckPoint == null) return true;
            return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayerMask);
        }

        /// <summary>
        /// Returns a random position within the arena boundaries.
        /// The Y position defaults to arena center Y.
        /// </summary>
        public Vector2 GetRandomArenaPosition()
        {
            float x = Random.Range(ArenaLeft + 1f, ArenaRight - 1f);
            return new Vector2(x, arenaCenter.y);
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private IEnumerator JumpCoroutine(Vector2 target)
        {
            _isJumping = true;

            if (_rb != null)
                _rb.velocity = new Vector2(_rb.velocity.x, jumpForce);

            // While airborne, steer toward target X
            float elapsed = 0f;
            while (elapsed < jumpAirTime)
            {
                elapsed += Time.deltaTime;
                if (_rb != null)
                {
                    float dirX = target.x - transform.position.x;
                    _rb.velocity = new Vector2(Mathf.Sign(dirX) * 8f, _rb.velocity.y);
                    if (_sr != null) _sr.flipX = dirX < 0f;
                }
                yield return null;
            }

            _isJumping = false;
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 center3 = new Vector3(arenaCenter.x, arenaCenter.y, 0f);
            Gizmos.DrawWireCube(center3, new Vector3(arenaWidth, 8f, 0f));

            Gizmos.color = Color.cyan;
            if (groundCheckPoint != null)
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
#endif
    }
}
