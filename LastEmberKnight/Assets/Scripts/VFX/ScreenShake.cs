// =============================================================================
//  ScreenShake.cs  –  Cinemachine-impulse screen shake for The Last Ember Knight
// =============================================================================
// Singleton MonoBehaviour using Cinemachine CinemachineImpulseSource.
//
// Presets:
//   Light  (0.10, 0.10)  – soft hit feedback
//   Medium (0.30, 0.20)  – normal hit / explosion
//   Heavy  (0.50, 0.40)  – strong hit / boss strike
//   Boss   (0.80, 0.60)  – boss special / phase transition
//
// Shake(magnitude, duration):  fire a custom impulse
// Camera punch: brief push toward a hit direction using AddForceAtPosition
// =============================================================================
using System.Collections;
using UnityEngine;

#if CINEMACHINE_INSTALLED
using Cinemachine;
#endif

namespace LastEmberKnight
{
    [DefaultExecutionOrder(-45)]
    public class ScreenShake : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static ScreenShake Instance { get; private set; }

        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Cinemachine Impulse (optional)")]
#if CINEMACHINE_INSTALLED
        [SerializeField] private CinemachineImpulseSource impulseSource;
#endif

        [Header("Fallback shake (no Cinemachine)")]
        [SerializeField] private Transform cameraOverrideTarget;  // if null, uses Camera.main

        // ─── Preset Definitions ───────────────────────────────────────────────────
        public static readonly ShakePreset Light  = new ShakePreset(0.10f, 0.10f);
        public static readonly ShakePreset Medium = new ShakePreset(0.30f, 0.20f);
        public static readonly ShakePreset Heavy  = new ShakePreset(0.50f, 0.40f);
        public static readonly ShakePreset Boss   = new ShakePreset(0.80f, 0.60f);

        // ─── Runtime ──────────────────────────────────────────────────────────────
        private Coroutine _fallbackCoroutine;
        private Transform _shakeTarget;
        private Vector3   _originalLocalPos;

        // ─────────────────────────────────────────────────────────────────────────
        #region Preset Struct

        public readonly struct ShakePreset
        {
            public readonly float Magnitude;
            public readonly float Duration;
            public ShakePreset(float magnitude, float duration)
            { Magnitude = magnitude; Duration = duration; }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Cache the camera transform for the fallback shake
            if (cameraOverrideTarget != null)
                _shakeTarget = cameraOverrideTarget;
            else if (Camera.main != null)
                _shakeTarget = Camera.main.transform;

            if (_shakeTarget != null)
                _originalLocalPos = _shakeTarget.localPosition;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Shake with a named preset.</summary>
        public void Shake(in ShakePreset preset)
            => Shake(preset.Magnitude, preset.Duration);

        /// <summary>Shake with a custom magnitude and duration.</summary>
        public void Shake(float magnitude, float duration)
        {
#if CINEMACHINE_INSTALLED
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulseWithForce(magnitude);
                return;
            }
#endif
            // Fallback: direct transform shake
            FallbackShake(magnitude, duration);
        }

        /// <summary>Light preset shake.</summary>
        public void ShakeLight()  => Shake(Light);

        /// <summary>Medium preset shake.</summary>
        public void ShakeMedium() => Shake(Medium);

        /// <summary>Heavy preset shake.</summary>
        public void ShakeHeavy()  => Shake(Heavy);

        /// <summary>Boss preset shake.</summary>
        public void ShakeBoss()   => Shake(Boss);

        /// <summary>
        /// Camera punch: briefly displaces the camera toward the hit direction then snaps back.
        /// Direction should be normalised or at least non-zero.
        /// </summary>
        public void CameraPunch(Vector2 direction, float magnitude = 0.12f, float duration = 0.08f)
        {
            if (_shakeTarget == null) return;
            if (_fallbackCoroutine != null) StopCoroutine(_fallbackCoroutine);
            _fallbackCoroutine = StartCoroutine(PunchCoroutine(direction.normalized, magnitude, duration));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Fallback Implementation

        private void FallbackShake(float magnitude, float duration)
        {
            if (_shakeTarget == null) return;
            if (_fallbackCoroutine != null) StopCoroutine(_fallbackCoroutine);
            _fallbackCoroutine = StartCoroutine(FallbackShakeCoroutine(magnitude, duration));
        }

        private IEnumerator FallbackShakeCoroutine(float magnitude, float duration)
        {
            if (_shakeTarget == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float falloff = 1f - elapsed / duration;
                float ox = Random.Range(-magnitude, magnitude) * falloff;
                float oy = Random.Range(-magnitude, magnitude) * falloff;
                _shakeTarget.localPosition = _originalLocalPos + new Vector3(ox, oy, 0f);
                yield return null;
            }
            _shakeTarget.localPosition = _originalLocalPos;
            _fallbackCoroutine = null;
        }

        private IEnumerator PunchCoroutine(Vector2 dir, float magnitude, float duration)
        {
            if (_shakeTarget == null) yield break;

            // Push toward dir
            float half = duration * 0.5f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / half;
                _shakeTarget.localPosition = _originalLocalPos + (Vector3)(dir * magnitude * (1f - t));
                yield return null;
            }
            // Return to origin
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / half;
                _shakeTarget.localPosition = Vector3.Lerp(_originalLocalPos + (Vector3)(dir * magnitude * 0.2f), _originalLocalPos, t);
                yield return null;
            }
            _shakeTarget.localPosition = _originalLocalPos;
            _fallbackCoroutine = null;
        }

        #endregion
    }
}
