// =============================================================================
//  CameraController.cs  –  Camera follow, screen shake, and punch impulse
// =============================================================================
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// 2D camera controller with:
    /// <list type="bullet">
    ///   <item>Smooth follow of the player with configurable lead.</item>
    ///   <item>Screen shake (delegates to <see cref="ScreenShake"/> for the actual
    ///         position offset so the two systems compose cleanly).</item>
    ///   <item>Camera "punch" impulse – a one-frame displacement in a given direction
    ///         used to emphasize hits (as seen in Street Fighter / Celeste).</item>
    /// </list>
    ///
    /// Static convenience methods (<see cref="RequestShake"/>, <see cref="RequestPunch"/>)
    /// can be called from any script without holding an instance reference.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // =====================================================================
        //  Singleton
        // =====================================================================

        public static CameraController Instance { get; private set; }

        // =====================================================================
        //  Inspector
        // =====================================================================

        [Header("Follow")]
        [SerializeField] private Transform  target;
        [SerializeField] private float      smoothTime        = 0.18f;
        [SerializeField] private Vector2    followOffset      = new Vector2(2f, 1f); // lead in facing direction
        [SerializeField] private float      zDepth            = -10f;

        [Header("Bounds (optional)")]
        [SerializeField] private bool       useBounds         = false;
        [SerializeField] private Vector2    boundsMin         = new Vector2(-50f, -20f);
        [SerializeField] private Vector2    boundsMax         = new Vector2( 50f,  20f);

        [Header("Punch")]
        [SerializeField] private float      punchDecaySpeed   = 10f;

        // =====================================================================
        //  Private state
        // =====================================================================

        private Vector3    _velocity     = Vector3.zero;
        private Vector2    _punchOffset  = Vector2.zero;
        private PlayerController _player;
        private Coroutine  _punchCoroutine;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Auto-find player if no target assigned
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstants.TagPlayer);
                if (playerObj != null)
                {
                    target  = playerObj.transform;
                    _player = playerObj.GetComponent<PlayerController>();
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // --- Desired position with directional lead ---
            float leadX = _player != null
                ? (_player.FacingRight ? followOffset.x : -followOffset.x)
                : 0f;

            Vector3 desired = new Vector3(
                target.position.x + leadX  + _punchOffset.x,
                target.position.y + followOffset.y + _punchOffset.y,
                zDepth);

            // --- Clamp to level bounds ---
            if (useBounds)
            {
                desired.x = Mathf.Clamp(desired.x, boundsMin.x, boundsMax.x);
                desired.y = Mathf.Clamp(desired.y, boundsMin.y, boundsMax.y);
            }

            // --- Smooth damp ---
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);

            // --- Decay punch offset ---
            _punchOffset = Vector2.MoveTowards(_punchOffset, Vector2.zero, punchDecaySpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        //  Screen shake – delegates to ScreenShake singleton
        // =====================================================================

        /// <summary>
        /// Trigger a screen shake.  If <see cref="ScreenShake.Instance"/> is present it
        /// is used; otherwise this component applies its own local offset.
        /// </summary>
        public static void RequestShake(float magnitude, float duration)
        {
            if (ScreenShake.Instance != null)
            {
                ScreenShake.Instance.Shake(magnitude, duration);
                return;
            }

            // Fallback: apply directly on this controller
            if (Instance != null)
                Instance.StartCoroutine(Instance.FallbackShake(magnitude, duration));
        }

        private IEnumerator FallbackShake(float magnitude, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t   = 1f - (elapsed / duration);
                float ox  = Random.Range(-magnitude, magnitude) * t;
                float oy  = Random.Range(-magnitude, magnitude) * t;
                _punchOffset = new Vector2(ox, oy);
                yield return null;
            }
            _punchOffset = Vector2.zero;
        }

        // =====================================================================
        //  Camera punch (hit-impact impulse)
        // =====================================================================

        /// <summary>
        /// Apply a brief directional camera impulse to emphasize a hit.
        /// </summary>
        /// <param name="direction">Normalised direction of the punch.</param>
        /// <param name="magnitude">Pixel-space offset magnitude in world units.</param>
        public static void RequestPunch(Vector2 direction, float magnitude)
        {
            if (Instance == null) return;
            Instance.ApplyPunch(direction, magnitude);
        }

        private void ApplyPunch(Vector2 direction, float magnitude)
        {
            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            _punchCoroutine = StartCoroutine(PunchRoutine(direction.normalized, magnitude));
        }

        private IEnumerator PunchRoutine(Vector2 dir, float magnitude)
        {
            // Instantly offset
            _punchOffset = dir * magnitude;

            // Let the LateUpdate decay handle the return – we just wait one frame
            // then make sure the offset is fully cleared after a short window.
            yield return new WaitForSecondsRealtime(0.1f);
            _punchOffset  = Vector2.zero;
            _punchCoroutine = null;
        }

        // =====================================================================
        //  Level bounds setter (called by LevelGenerator)
        // =====================================================================

        /// <summary>Set the camera's clamping bounds for the current level.</summary>
        public static void SetLevelBounds(Vector2 min, Vector2 max)
        {
            if (Instance == null) return;
            Instance.useBounds = true;
            Instance.boundsMin = min;
            Instance.boundsMax = max;
        }

        /// <summary>Disable level-bounds clamping (e.g. for title / end screens).</summary>
        public static void ClearBounds()
        {
            if (Instance == null) return;
            Instance.useBounds = false;
        }

        // =====================================================================
        //  Immediate snap (used on scene load to avoid camera sliding in)
        // =====================================================================

        /// <summary>
        /// Teleport the camera instantly to the target position (no smoothing).
        /// Call this once after a level is loaded so the camera starts centred.
        /// </summary>
        public static void SnapToTarget()
        {
            if (Instance == null || Instance.target == null) return;
            Instance.transform.position = new Vector3(
                Instance.target.position.x,
                Instance.target.position.y,
                Instance.zDepth);
            Instance._velocity = Vector3.zero;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug – Trigger Test Shake")]
        private void Debug_Shake() => RequestShake(0.3f, 0.4f);

        [ContextMenu("Debug – Snap To Target")]
        private void Debug_Snap() => SnapToTarget();

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;
            Gizmos.color = Color.cyan;
            Vector2 size  = boundsMax - boundsMin;
            Vector2 center = (boundsMin + boundsMax) * 0.5f;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));
        }
#endif
    }
}
