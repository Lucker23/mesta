using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Singleton that provides hit-stop (time-freeze) and camera impulse feedback on hits.
    /// Two modes are available:
    ///   1. RequestStop(frames)  – freezes Time.timeScale for N frames then restores it.
    ///   2. FreezeObject(obj, frames) – pauses a specific GameObject's Rigidbody2D/Animator.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static HitStop Instance { get; private set; }

        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Global Hit Stop")]
        [SerializeField] private float frozenTimeScale   = 0f;
        [SerializeField] private float restoreTimeScale  = 1f;

        [Header("Camera Impulse")]
        [SerializeField] private float cameraImpulseMagnitude = 0.1f;
        [SerializeField] private float cameraImpulseDuration  = 0.08f;

        // ── Runtime ────────────────────────────────────────────────────────────
        private bool isGlobalStopped;
        private float savedTimeScale;

        // Per-object freeze tracking
        private readonly Dictionary<int, Coroutine> frozenObjects = new Dictionary<int, Coroutine>();

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                // Restore time scale in case we were frozen
                if (isGlobalStopped)
                    Time.timeScale = restoreTimeScale;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Freeze Time.timeScale for the given number of frames (uses unscaled time).
        /// Safe to call multiple times; the last call wins if already frozen.
        /// </summary>
        public void RequestStop(int frames)
        {
            if (frames <= 0) return;

            // Cancel any in-progress global stop
            StopAllCoroutines();
            StartCoroutine(GlobalStopCoroutine(frames));

            // Fire camera impulse
            StartCoroutine(CameraImpulse());
        }

        /// <summary>
        /// Freeze a specific GameObject (its Rigidbody2D and Animator) for N frames.
        /// Useful for freezing only the hit enemy without pausing the whole game.
        /// </summary>
        public void FreezeObject(GameObject obj, int frames)
        {
            if (obj == null || frames <= 0) return;

            int id = obj.GetInstanceID();

            // Stop any existing freeze coroutine for this object
            if (frozenObjects.TryGetValue(id, out Coroutine existing) && existing != null)
                StopCoroutine(existing);

            Coroutine co = StartCoroutine(FreezeObjectCoroutine(obj, frames));
            frozenObjects[id] = co;
        }

        // ── Coroutines ────────────────────────────────────────────────────────

        private IEnumerator GlobalStopCoroutine(int frames)
        {
            isGlobalStopped = true;
            savedTimeScale  = Time.timeScale;
            Time.timeScale  = frozenTimeScale;

            // Wait using unscaled time so the freeze actually works
            float frameDuration = Time.fixedDeltaTime; // ~0.02s per frame
            float waitTime      = frames * frameDuration;
            yield return new WaitForSecondsRealtime(waitTime);

            Time.timeScale  = savedTimeScale;
            isGlobalStopped = false;
        }

        private IEnumerator FreezeObjectCoroutine(GameObject obj, int frames)
        {
            if (obj == null) yield break;

            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            Animator    an = obj.GetComponent<Animator>();

            // Save state
            Vector2 savedVelocity  = rb != null ? rb.velocity : Vector2.zero;
            float   savedAngularVel = rb != null ? rb.angularVelocity : 0f;
            float   savedAnimSpeed  = an != null ? an.speed : 1f;

            // Pause
            if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; rb.isKinematic = true; }
            if (an != null) an.speed = 0f;

            float waitTime = frames * Time.fixedDeltaTime;
            yield return new WaitForSecondsRealtime(waitTime);

            // Restore — only if the object still exists
            if (obj != null)
            {
                if (rb != null) { rb.isKinematic = false; rb.velocity = savedVelocity; rb.angularVelocity = savedAngularVel; }
                if (an != null) an.speed = savedAnimSpeed;
            }

            frozenObjects.Remove(obj != null ? obj.GetInstanceID() : -1);
        }

        private IEnumerator CameraImpulse()
        {
            if (Camera.main == null) yield break;

            Vector3 originalPos = Camera.main.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < cameraImpulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float x = UnityEngine.Random.Range(-cameraImpulseMagnitude, cameraImpulseMagnitude);
                float y = UnityEngine.Random.Range(-cameraImpulseMagnitude, cameraImpulseMagnitude);
                Camera.main.transform.localPosition = originalPos + new Vector3(x, y, 0f);
                yield return null;
            }

            Camera.main.transform.localPosition = originalPos;
        }
    }
}
