using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LastEmberKnight
{
    /// <summary>
    /// Handles all visual and audio effects for boss phase transitions.
    /// Attach to the same GameObject as (or a child of) a BossBase.
    /// Features: screen flash, camera zoom, invincibility window, particle explosion.
    /// </summary>
    public class BossPhaseManager : MonoBehaviour
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Screen Flash")]
        [SerializeField] private Image screenFlashImage;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashPeakDuration = 0.1f;
        [SerializeField] private float flashFadeInDuration = 0.05f;
        [SerializeField] private float flashFadeOutDuration = 0.4f;

        [Header("Camera Zoom")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float zoomInSize = 3.5f;
        [SerializeField] private float defaultSize = 5f;
        [SerializeField] private float zoomInDuration = 0.3f;
        [SerializeField] private float zoomHoldDuration = 0.5f;
        [SerializeField] private float zoomOutDuration = 0.6f;

        [Header("Transition Invincibility")]
        [SerializeField] private float invincibilityDuration = 1.5f;

        [Header("Particle Explosion")]
        [SerializeField] private ParticleSystem explosionParticles;
        [SerializeField] private int particleBurstCount = 60;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 0.4f;
        [SerializeField] private float shakeMagnitude = 0.25f;

        // ── Runtime State ──────────────────────────────────────────────────

        private bool _transitioning = false;
        private Vector3 _cameraOriginalPos;

        // ── Unity Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera != null)
            {
                defaultSize = targetCamera.orthographicSize;
                _cameraOriginalPos = targetCamera.transform.position;
            }

            // Ensure flash image is invisible at start
            if (screenFlashImage != null)
            {
                Color c = flashColor;
                c.a = 0f;
                screenFlashImage.color = c;
                screenFlashImage.gameObject.SetActive(false);
            }
        }

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Triggers the full phase transition sequence: flash, zoom, particles, shake.
        /// The boss will already have set isInvincible = true via BossBase.PhaseTransitionSequence.
        /// </summary>
        public void TriggerPhaseTransition(BossBase boss)
        {
            if (_transitioning) return;
            StartCoroutine(PhaseTransitionSequence(boss));
        }

        // ── Coroutine ──────────────────────────────────────────────────────

        private IEnumerator PhaseTransitionSequence(BossBase boss)
        {
            _transitioning = true;

            // Run flash, zoom, and shake in parallel
            StartCoroutine(ScreenFlashCoroutine());
            StartCoroutine(CameraZoomCoroutine());
            StartCoroutine(CameraShakeCoroutine());

            // Trigger particle explosion immediately
            TriggerParticleExplosion(boss.transform.position);

            // Wait for the full invincibility window before releasing
            yield return new WaitForSeconds(invincibilityDuration);

            _transitioning = false;
        }

        // ── Screen Flash ───────────────────────────────────────────────────

        private IEnumerator ScreenFlashCoroutine()
        {
            if (screenFlashImage == null) yield break;

            screenFlashImage.gameObject.SetActive(true);

            // Fade in
            yield return StartCoroutine(FadeImage(screenFlashImage, 0f, 1f, flashFadeInDuration));

            // Hold at peak
            yield return new WaitForSeconds(flashPeakDuration);

            // Fade out
            yield return StartCoroutine(FadeImage(screenFlashImage, 1f, 0f, flashFadeOutDuration));

            screenFlashImage.gameObject.SetActive(false);
        }

        private IEnumerator FadeImage(Image image, float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            Color c = flashColor;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
                image.color = c;
                yield return null;
            }

            c.a = toAlpha;
            image.color = c;
        }

        // ── Camera Zoom ────────────────────────────────────────────────────

        private IEnumerator CameraZoomCoroutine()
        {
            if (targetCamera == null) yield break;

            // Zoom in
            float start = targetCamera.orthographicSize;
            float elapsed = 0f;
            while (elapsed < zoomInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                targetCamera.orthographicSize = Mathf.Lerp(start, zoomInSize,
                    Mathf.SmoothStep(0f, 1f, elapsed / zoomInDuration));
                yield return null;
            }
            targetCamera.orthographicSize = zoomInSize;

            // Hold
            yield return new WaitForSecondsRealtime(zoomHoldDuration);

            // Zoom out
            elapsed = 0f;
            while (elapsed < zoomOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                targetCamera.orthographicSize = Mathf.Lerp(zoomInSize, defaultSize,
                    Mathf.SmoothStep(0f, 1f, elapsed / zoomOutDuration));
                yield return null;
            }
            targetCamera.orthographicSize = defaultSize;
        }

        // ── Camera Shake ───────────────────────────────────────────────────

        private IEnumerator CameraShakeCoroutine()
        {
            if (targetCamera == null) yield break;

            _cameraOriginalPos = targetCamera.transform.position;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / shakeDuration;
                float currentMagnitude = Mathf.Lerp(shakeMagnitude, 0f, progress);

                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f) * currentMagnitude,
                    UnityEngine.Random.Range(-1f, 1f) * currentMagnitude,
                    0f);

                targetCamera.transform.position = _cameraOriginalPos + offset;
                yield return null;
            }

            targetCamera.transform.position = _cameraOriginalPos;
        }

        // ── Particle Explosion ─────────────────────────────────────────────

        private void TriggerParticleExplosion(Vector3 position)
        {
            if (explosionParticles != null)
            {
                explosionParticles.transform.position = position;
                var emission = explosionParticles.emission;
                emission.enabled = true;
                explosionParticles.Emit(particleBurstCount);
            }
        }

        // ── Manual Reset (e.g. for testing) ───────────────────────────────

        public void ResetCamera()
        {
            if (targetCamera != null)
            {
                targetCamera.orthographicSize = defaultSize;
                targetCamera.transform.position = _cameraOriginalPos;
            }
        }
    }
}
