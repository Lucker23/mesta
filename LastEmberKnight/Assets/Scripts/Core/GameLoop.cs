// =============================================================================
//  GameLoop.cs  –  Update/FixedUpdate routing, pause, and hit-stop management
// =============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Central MonoBehaviour that owns:
    /// <list type="bullet">
    ///   <item><b>Pause</b>  – sets <c>Time.timeScale = 0</c>, restores pre-pause scale on resume.</item>
    ///   <item><b>HitStop</b> – freezes time for N real-time fixed-update frames on a hit.
    ///         Multiple requests are coalesced (the largest wins).  Implemented with
    ///         <c>WaitForSecondsRealtime</c> so the freeze is immune to Time.timeScale.</item>
    ///   <item><b>SlowMotion</b> – brief slowdown (e.g. on a parry).</item>
    ///   <item><b>IUpdateable / IFixedUpdateable</b> – opt-in hook for systems that need a
    ///         managed tick rather than their own MonoBehaviour.</item>
    /// </list>
    ///
    /// Call the static entry-points from anywhere:
    /// <code>
    ///   GameLoop.RequestHitStop(3);
    ///   GameLoop.RequestSlowMotion(0.25f, 0.3f);
    /// </code>
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class GameLoop : MonoBehaviour
    {
        // =====================================================================
        //  Managed-update interfaces
        // =====================================================================

        /// <summary>Systems that want a managed Update tick register this interface.</summary>
        public interface IUpdateable      { void ManagedUpdate(); }

        /// <summary>Systems that want a managed FixedUpdate tick register this interface.</summary>
        public interface IFixedUpdateable { void ManagedFixedUpdate(); }

        // =====================================================================
        //  Singleton
        // =====================================================================

        public static GameLoop Instance { get; private set; }

        // =====================================================================
        //  Inspector
        // =====================================================================

        [Header("Hit-Stop")]
        [Tooltip("Time scale applied during a hit-stop freeze (0 = fully frozen).")]
        [SerializeField] [Range(0f, 0.5f)] private float hitStopTimeScale = 0f;

        [Header("Slow-Motion Defaults")]
        [Tooltip("Default slow-motion time-scale multiplier.")]
        [SerializeField] [Range(0.05f, 0.99f)] private float defaultSlowMotionScale    = 0.3f;

        [Tooltip("Default slow-motion duration in seconds.")]
        [SerializeField] [Range(0.05f, 2f)]    private float defaultSlowMotionDuration = 0.25f;

        // =====================================================================
        //  Managed-update subscriber lists
        // =====================================================================

        private readonly List<IUpdateable>      _updateables      = new List<IUpdateable>();
        private readonly List<IFixedUpdateable> _fixedUpdateables = new List<IFixedUpdateable>();

        // =====================================================================
        //  Pause state
        // =====================================================================

        private bool  _isPaused;
        private float _prePauseTimeScale = 1f;

        // =====================================================================
        //  HitStop state
        // =====================================================================

        private int       _hitStopFramesPending;   // largest outstanding request
        private bool      _hitStopActive;
        private Coroutine _hitStopCoroutine;

        // =====================================================================
        //  SlowMotion state
        // =====================================================================

        private Coroutine _slowMotionCoroutine;

        // =====================================================================
        //  Public accessors
        // =====================================================================

        public bool IsPaused     => _isPaused;
        public bool IsHitStopped => _hitStopActive;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_isPaused) return;

            // Dispatch managed Update to registered subscribers
            for (int i = _updateables.Count - 1; i >= 0; i--)
            {
                if (_updateables[i] == null)
                    _updateables.RemoveAt(i);
                else
                    _updateables[i].ManagedUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (_isPaused) return;

            // Dispatch managed FixedUpdate to registered subscribers
            for (int i = _fixedUpdateables.Count - 1; i >= 0; i--)
            {
                if (_fixedUpdateables[i] == null)
                    _fixedUpdateables.RemoveAt(i);
                else
                    _fixedUpdateables[i].ManagedFixedUpdate();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        //  Subscriber registration
        // =====================================================================

        /// <summary>Register a system to receive managed Update ticks.</summary>
        public void RegisterUpdateable(IUpdateable target)
        {
            if (!_updateables.Contains(target))
                _updateables.Add(target);
        }

        /// <summary>Unregister a managed Update subscriber.</summary>
        public void UnregisterUpdateable(IUpdateable target)
        {
            _updateables.Remove(target);
        }

        /// <summary>Register a system to receive managed FixedUpdate ticks.</summary>
        public void RegisterFixedUpdateable(IFixedUpdateable target)
        {
            if (!_fixedUpdateables.Contains(target))
                _fixedUpdateables.Add(target);
        }

        /// <summary>Unregister a managed FixedUpdate subscriber.</summary>
        public void UnregisterFixedUpdateable(IFixedUpdateable target)
        {
            _fixedUpdateables.Remove(target);
        }

        // =====================================================================
        //  Pause API
        // =====================================================================

        /// <summary>
        /// Pause the game by setting <c>Time.timeScale = 0</c>.
        /// The pre-pause scale is stored and restored on <see cref="Resume"/>.
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;
            _isPaused          = true;
            _prePauseTimeScale = Time.timeScale;
            Time.timeScale     = 0f;
        }

        /// <summary>
        /// Resume from pause, restoring the pre-pause time scale.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused      = false;
            Time.timeScale = _prePauseTimeScale;
        }

        // =====================================================================
        //  HitStop API  –  static convenience wrappers
        // =====================================================================

        /// <summary>
        /// Freeze time for <paramref name="frames"/> real fixed-update frames.
        /// Multiple simultaneous requests are coalesced: the largest value wins.
        /// Safe to call from any context (uses the singleton instance).
        /// </summary>
        /// <param name="frames">Number of fixed-update frames to freeze (min 1).</param>
        public static void RequestHitStop(int frames)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[GameLoop] RequestHitStop: no GameLoop instance in scene.");
                return;
            }
            Instance.AddHitStop(frames);
        }

        /// <summary>
        /// Trigger a brief slow-motion window.
        /// Pass -1 for <paramref name="duration"/> or <paramref name="scale"/> to use
        /// the inspector defaults.
        /// </summary>
        /// <param name="duration">Slow-motion duration in seconds (-1 = default).</param>
        /// <param name="scale">Time scale during slow-motion, 0.05–0.99 (-1 = default).</param>
        public static void RequestSlowMotion(float duration = -1f, float scale = -1f)
        {
            if (Instance == null) return;
            float d = duration < 0f ? Instance.defaultSlowMotionDuration : duration;
            float s = scale    < 0f ? Instance.defaultSlowMotionScale    : scale;
            Instance.TriggerSlowMotion(d, s);
        }

        // =====================================================================
        //  Internal HitStop implementation
        // =====================================================================

        private void AddHitStop(int frames)
        {
            if (frames <= 0) return;

            // Coalesce: keep the largest pending request
            _hitStopFramesPending = Mathf.Max(_hitStopFramesPending, frames);

            if (!_hitStopActive)
            {
                if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
                _hitStopCoroutine = StartCoroutine(RunHitStop());
            }
        }

        private IEnumerator RunHitStop()
        {
            while (_hitStopFramesPending > 0)
            {
                _hitStopActive = true;

                // Capture and consume current request count
                int framesToFreeze      = _hitStopFramesPending;
                _hitStopFramesPending   = 0;

                // Save current scale (handle nested pause gracefully)
                float savedScale = _isPaused ? _prePauseTimeScale : Time.timeScale;

                if (!_isPaused)
                    Time.timeScale = hitStopTimeScale;

                // Wait using real time so the freeze is impervious to timeScale itself
                float frameDuration = Time.fixedDeltaTime; // real-time frame length
                for (int i = 0; i < framesToFreeze; i++)
                    yield return new WaitForSecondsRealtime(frameDuration);

                // Restore – be careful not to overwrite a pause that started mid-stop
                if (_isPaused)
                    _prePauseTimeScale = savedScale;   // will be applied when Resume() is called
                else
                    Time.timeScale = savedScale;

                // If a new request arrived while we were frozen, loop and handle it
            }

            _hitStopActive    = false;
            _hitStopCoroutine = null;
        }

        // =====================================================================
        //  Internal SlowMotion implementation
        // =====================================================================

        private void TriggerSlowMotion(float duration, float scale)
        {
            // Cancel any running slow-motion
            if (_slowMotionCoroutine != null)
                StopCoroutine(_slowMotionCoroutine);

            _slowMotionCoroutine = StartCoroutine(RunSlowMotion(duration, scale));
        }

        private IEnumerator RunSlowMotion(float duration, float scale)
        {
            if (!_isPaused)
                Time.timeScale = Mathf.Clamp(scale, 0.01f, 1f);

            yield return new WaitForSecondsRealtime(duration);

            if (!_isPaused)
                Time.timeScale = 1f;

            _slowMotionCoroutine = null;
        }

        // =====================================================================
        //  Emergency reset
        // =====================================================================

        /// <summary>
        /// Immediately cancel all ongoing time-scale overrides and restore normal speed.
        /// Use when loading a new scene or after an unrecoverable error.
        /// </summary>
        public void ForceNormalSpeed()
        {
            _isPaused             = false;
            _hitStopActive        = false;
            _hitStopFramesPending = 0;

            if (_hitStopCoroutine   != null) { StopCoroutine(_hitStopCoroutine);   _hitStopCoroutine   = null; }
            if (_slowMotionCoroutine != null) { StopCoroutine(_slowMotionCoroutine); _slowMotionCoroutine = null; }

            Time.timeScale = 1f;
        }

        // =====================================================================
        //  Editor helpers
        // =====================================================================

#if UNITY_EDITOR
        [ContextMenu("Debug – 8-frame HitStop")]
        private void Debug_HitStop8() => RequestHitStop(8);

        [ContextMenu("Debug – Slow Motion (default)")]
        private void Debug_SlowMotion() => RequestSlowMotion();

        [ContextMenu("Debug – Force Normal Speed")]
        private void Debug_ForceNormal() => ForceNormalSpeed();

        [ContextMenu("Debug – Print State")]
        private void Debug_PrintState()
        {
            Debug.Log($"[GameLoop] Paused={_isPaused}  HitStop={_hitStopActive}  " +
                      $"PendingFrames={_hitStopFramesPending}  TimeScale={Time.timeScale}");
        }
#endif
    }
}
