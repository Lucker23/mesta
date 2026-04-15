// =============================================================================
//  HitEffects.cs  –  Sprite hit flash, invincibility flicker, and death dissolve
// =============================================================================
// MonoBehaviour that other systems call to apply visual feedback to sprites.
//
// FlashWhite(sr, duration)            – 0.05 s white sprite flash on hit
// ShowInvincibilityFlicker(sr)        – alpha flicker loop during i-frames
// StopInvincibilityFlicker(sr)        – ends the flicker, restores alpha
// DissolveOut(sr, duration)           – fades alpha to 0 over duration (death)
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    public class HitEffects : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static HitEffects Instance { get; private set; }

        // ─── Constants ────────────────────────────────────────────────────────────
        private const float WHITE_FLASH_DURATION = 0.05f;
        private const float IFRAMES_FLICKER_INTERVAL = 0.06f;  // seconds between flicker toggles
        private const float DEATH_DISSOLVE_DURATION = 0.30f;

        // Cache of active coroutines per SpriteRenderer to allow cancellation
        private readonly Dictionary<SpriteRenderer, Coroutine> _activeFlashes   = new Dictionary<SpriteRenderer, Coroutine>();
        private readonly Dictionary<SpriteRenderer, Coroutine> _activeFlickers  = new Dictionary<SpriteRenderer, Coroutine>();
        private readonly Dictionary<SpriteRenderer, Coroutine> _activeDissolves = new Dictionary<SpriteRenderer, Coroutine>();

        // Pre-allocated white material cache so we don't allocate per-frame
        private readonly Dictionary<SpriteRenderer, Material> _originalMaterials = new Dictionary<SpriteRenderer, Material>();
        private Material _whiteMaterial;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Build a white sprite material once
            _whiteMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Sprites/Diffuse"));
            _whiteMaterial.color = Color.white;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// Briefly flash a sprite white for <paramref name="duration"/> seconds.
        /// Default duration is 0.05 s (one hit flash).
        /// </summary>
        public void FlashWhite(SpriteRenderer sr, float duration = WHITE_FLASH_DURATION)
        {
            if (sr == null) return;

            // Cancel any in-progress flash on this renderer
            if (_activeFlashes.TryGetValue(sr, out Coroutine prev) && prev != null)
                StopCoroutine(prev);

            _activeFlashes[sr] = StartCoroutine(FlashWhiteCoroutine(sr, duration));
        }

        /// <summary>
        /// Begin the alpha flicker loop used during invincibility frames.
        /// Stops automatically only when <see cref="StopInvincibilityFlicker"/> is called.
        /// </summary>
        public void ShowInvincibilityFlicker(SpriteRenderer sr)
        {
            if (sr == null) return;
            StopInvincibilityFlicker(sr);   // cancel any previous
            _activeFlickers[sr] = StartCoroutine(IFrameFlickerCoroutine(sr));
        }

        /// <summary>
        /// End the invincibility flicker and restore the sprite's alpha to 1.
        /// </summary>
        public void StopInvincibilityFlicker(SpriteRenderer sr)
        {
            if (sr == null) return;
            if (_activeFlickers.TryGetValue(sr, out Coroutine co) && co != null)
                StopCoroutine(co);
            _activeFlickers.Remove(sr);
            if (sr != null) SetAlpha(sr, 1f);
        }

        /// <summary>
        /// Fade the sprite's alpha from 1 to 0 over <paramref name="duration"/> seconds
        /// (used for enemy/player death dissolve). Default is 0.3 s.
        /// </summary>
        public void DissolveOut(SpriteRenderer sr, float duration = DEATH_DISSOLVE_DURATION)
        {
            if (sr == null) return;

            if (_activeDissolves.TryGetValue(sr, out Coroutine prev) && prev != null)
                StopCoroutine(prev);

            _activeDissolves[sr] = StartCoroutine(DissolveCoroutine(sr, duration));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Coroutines

        private IEnumerator FlashWhiteCoroutine(SpriteRenderer sr, float duration)
        {
            if (sr == null) yield break;

            // Cache original material
            if (!_originalMaterials.ContainsKey(sr))
                _originalMaterials[sr] = sr.material;

            // Swap to white material
            sr.material = _whiteMaterial;

            yield return new WaitForSeconds(duration);

            // Restore original material
            if (sr != null)
            {
                if (_originalMaterials.TryGetValue(sr, out Material origMat))
                    sr.material = origMat;
            }

            _activeFlashes.Remove(sr);
        }

        private IEnumerator IFrameFlickerCoroutine(SpriteRenderer sr)
        {
            if (sr == null) yield break;
            bool visible = true;
            while (true)
            {
                visible = !visible;
                SetAlpha(sr, visible ? 1f : 0f);
                yield return new WaitForSeconds(IFRAMES_FLICKER_INTERVAL);
            }
        }

        private IEnumerator DissolveCoroutine(SpriteRenderer sr, float duration)
        {
            if (sr == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                SetAlpha(sr, alpha);
                yield return null;
            }
            SetAlpha(sr, 0f);
            _activeDissolves.Remove(sr);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Helpers

        private static void SetAlpha(SpriteRenderer sr, float alpha)
        {
            if (sr == null) return;
            Color c = sr.color; c.a = alpha; sr.color = c;
        }

        #endregion
    }
}
