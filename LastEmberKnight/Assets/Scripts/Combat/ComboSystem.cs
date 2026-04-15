using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace LastEmberKnight
{
    /// <summary>
    /// Tracks player combo state. Attach to the PlayerCombat GameObject.
    /// Exposes ComboMultiplier for use by attack code.
    /// </summary>
    public class ComboSystem : MonoBehaviour
    {
        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Combo Timing")]
        [SerializeField] private float comboWindow = 0.6f;   // seconds between hits to extend combo

        [Header("Multipliers (hit 1, 2, 3+)")]
        [SerializeField] private float mult1 = 1.0f;   // hits 1
        [SerializeField] private float mult2 = 1.1f;   // hits 2
        [SerializeField] private float mult3 = 1.3f;   // hits 3+

        [Header("UI")]
        [SerializeField] private TMP_Text comboCountLabel;   // e.g. "3x COMBO"
        [SerializeField] private TMP_Text comboMultLabel;    // e.g. "x1.3"
        [SerializeField] private CanvasGroup comboCanvasGroup;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action<int, float> OnComboUpdated;    // (hitCount, multiplier)
        public event Action             OnComboReset;

        // ── Runtime State ─────────────────────────────────────────────────────
        private int    comboCount;
        private float  comboTimer;
        private bool   comboActive;
        private Coroutine fadeCoroutine;

        // ── Properties ────────────────────────────────────────────────────────
        public int   ComboCount      => comboCount;
        public float ComboMultiplier => GetMultiplier(comboCount);
        public bool  IsComboActive   => comboActive;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Update()
        {
            if (!comboActive) return;

            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                ResetCombo();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Call this when the player lands a hit.</summary>
        public void RegisterHit()
        {
            comboCount++;
            comboTimer  = comboWindow;
            comboActive = true;

            float mult = GetMultiplier(comboCount);
            OnComboUpdated?.Invoke(comboCount, mult);
            UpdateUI();
        }

        /// <summary>Call this when the player misses (attack whiff) or takes damage.</summary>
        public void ResetCombo()
        {
            if (comboCount == 0) return;
            comboCount  = 0;
            comboTimer  = 0f;
            comboActive = false;

            OnComboReset?.Invoke();
            HideUI();
        }

        // ── Multiplier Logic ──────────────────────────────────────────────────
        private float GetMultiplier(int count)
        {
            if (count >= 3) return mult3;
            if (count == 2) return mult2;
            return mult1;
        }

        // ── UI Helpers ────────────────────────────────────────────────────────
        private void UpdateUI()
        {
            if (comboCanvasGroup != null)
            {
                comboCanvasGroup.alpha = 1f;

                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            }

            if (comboCountLabel != null)
                comboCountLabel.text = $"{comboCount}x COMBO";

            if (comboMultLabel != null)
                comboMultLabel.text = $"x{GetMultiplier(comboCount):F1}";

            // Pop scale animation
            StartCoroutine(PopUI());
        }

        private void HideUI()
        {
            if (comboCanvasGroup == null) return;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutUI());
        }

        private IEnumerator PopUI()
        {
            if (comboCountLabel == null) yield break;
            Transform t = comboCountLabel.transform;
            Vector3 baseScale = Vector3.one;
            t.localScale = baseScale * 1.4f;

            float elapsed = 0f;
            float duration = 0.12f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(baseScale * 1.4f, baseScale, elapsed / duration);
                yield return null;
            }
            t.localScale = baseScale;
        }

        private IEnumerator FadeOutUI()
        {
            if (comboCanvasGroup == null) yield break;
            float elapsed = 0f;
            float duration = 0.4f;
            float startAlpha = comboCanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                comboCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }

            comboCanvasGroup.alpha = 0f;
        }
    }
}
