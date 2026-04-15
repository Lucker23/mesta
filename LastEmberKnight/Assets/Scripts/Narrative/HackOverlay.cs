// =============================================================================
//  HackOverlay.cs  –  CRT / terminal visual overlay for Lucker23 events
// =============================================================================
// Creates a retro-terminal aesthetic over the game screen:
//   - Scanlines: array of thin black horizontal Image strips
//   - Green tint overlay (CanvasGroup + Image)
//   - Screen flicker: random alpha spikes
//   - Terminal text: TextMeshPro types out character by character
//
// Public API:
//   EnableOverlay(color, intensity)             – fade in the tinted overlay
//   DisableOverlay(fadeTime)                    – fade out
//   ShowTerminalLine(text, delay)               – type out a line after delay
//   ClearTerminal()                             – wipe all terminal lines
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    public class HackOverlay : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static HackOverlay Instance { get; private set; }

        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Overlay Root")]
        [SerializeField] private CanvasGroup overlayRoot;
        [SerializeField] private Image       colorTintImage;

        [Header("Scanlines")]
        [SerializeField] private RectTransform scanlineContainer;
        [SerializeField] private int            scanlineCount  = 60;
        [SerializeField] private float          scanlineHeight = 2f;
        [SerializeField] private float          scanlineAlpha  = 0.35f;

        [Header("Terminal Text")]
        [SerializeField] private RectTransform         terminalContainer;
        [SerializeField] private TextMeshProUGUI        terminalLinePrefab;
        [SerializeField] private int                    maxVisibleLines = 8;

        [Header("Flicker")]
        [SerializeField] private float flickerMinInterval = 0.04f;
        [SerializeField] private float flickerMaxInterval = 0.18f;
        [SerializeField] private float flickerStrength    = 0.25f;

        // ─── Typing Speed ─────────────────────────────────────────────────────────
        private const float CHARS_PER_SECOND = 28f;

        // ─── Runtime ──────────────────────────────────────────────────────────────
        private readonly List<TextMeshProUGUI> _terminalLines = new List<TextMeshProUGUI>();
        private Coroutine _flickerCoroutine;
        private bool      _isActive;
        private Color     _overlayColor;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (overlayRoot != null) { overlayRoot.alpha = 0f; overlayRoot.blocksRaycasts = false; }
            BuildScanlines();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Fade in the coloured overlay with the given tint and alpha intensity.</summary>
        public void EnableOverlay(Color color, float intensity = 0.55f)
        {
            _overlayColor = color;
            _isActive     = true;

            if (colorTintImage != null)
                colorTintImage.color = new Color(color.r, color.g, color.b, intensity);

            StopAllCoroutines();
            StartCoroutine(FadeOverlay(0f, 1f, 0.25f));
            _flickerCoroutine = StartCoroutine(FlickerCoroutine());
        }

        /// <summary>Fade out and disable the overlay.</summary>
        public void DisableOverlay(float fadeTime = 0.5f)
        {
            _isActive = false;
            StopAllCoroutines();
            StartCoroutine(DisableAfterFade(fadeTime));
        }

        /// <summary>
        /// Type out one terminal line after <paramref name="delay"/> seconds.
        /// Each character appears at <see cref="CHARS_PER_SECOND"/> speed.
        /// </summary>
        public Coroutine ShowTerminalLine(string text, float delay = 0f)
        {
            return StartCoroutine(TypeTerminalLine(text, delay));
        }

        /// <summary>Remove all terminal lines from the screen.</summary>
        public void ClearTerminal()
        {
            foreach (TextMeshProUGUI line in _terminalLines)
                if (line != null) Destroy(line.gameObject);
            _terminalLines.Clear();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Scanline Construction

        private void BuildScanlines()
        {
            if (scanlineContainer == null) return;

            // Clear any designer-placed children
            foreach (Transform child in scanlineContainer)
                Destroy(child.gameObject);

            float totalHeight = scanlineContainer.rect.height;
            if (totalHeight <= 0f) totalHeight = Screen.height;

            float gap = totalHeight / scanlineCount;

            for (int i = 0; i < scanlineCount; i++)
            {
                GameObject go = new GameObject("Scanline_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(scanlineContainer, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin  = new Vector2(0f, 0f);
                rt.anchorMax  = new Vector2(1f, 0f);
                rt.pivot      = new Vector2(0.5f, 0f);
                rt.sizeDelta  = new Vector2(0f, scanlineHeight);
                rt.anchoredPosition = new Vector2(0f, i * gap);

                Image img  = go.GetComponent<Image>();
                img.color  = new Color(0f, 0f, 0f, scanlineAlpha);
                img.raycastTarget = false;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Coroutines

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            if (overlayRoot == null) yield break;
            float elapsed = 0f;
            overlayRoot.blocksRaycasts = false;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                overlayRoot.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            overlayRoot.alpha = to;
        }

        private IEnumerator DisableAfterFade(float fadeTime)
        {
            if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
            yield return FadeOverlay(overlayRoot != null ? overlayRoot.alpha : 1f, 0f, fadeTime);
            if (overlayRoot != null) overlayRoot.blocksRaycasts = false;
        }

        private IEnumerator FlickerCoroutine()
        {
            while (_isActive)
            {
                float wait = Random.Range(flickerMinInterval, flickerMaxInterval);
                yield return new WaitForSeconds(wait);

                if (overlayRoot == null) continue;
                float baseAlpha  = overlayRoot.alpha;
                float flickAlpha = baseAlpha + Random.Range(-flickerStrength, flickerStrength);
                overlayRoot.alpha = Mathf.Clamp01(flickAlpha);

                yield return new WaitForSeconds(0.03f);
                overlayRoot.alpha = baseAlpha;
            }
        }

        private IEnumerator TypeTerminalLine(string fullText, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            // Evict oldest line if at max
            if (_terminalLines.Count >= maxVisibleLines && _terminalLines.Count > 0)
            {
                TextMeshProUGUI oldest = _terminalLines[0];
                _terminalLines.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            // Instantiate a new line
            TextMeshProUGUI newLine;
            if (terminalLinePrefab != null && terminalContainer != null)
            {
                newLine = Instantiate(terminalLinePrefab, terminalContainer);
            }
            else
            {
                // Fallback: create a basic TMP object
                GameObject go = new GameObject("TerminalLine", typeof(RectTransform));
                go.transform.SetParent(terminalContainer != null ? terminalContainer : transform, false);
                newLine = go.AddComponent<TextMeshProUGUI>();
                newLine.fontSize  = 18f;
                newLine.color     = new Color(0.15f, 1f, 0.15f, 1f);   // green
                newLine.fontStyle = FontStyles.Normal;
            }

            newLine.text = "";
            _terminalLines.Add(newLine);

            // Type out character by character
            float charInterval = 1f / CHARS_PER_SECOND;
            foreach (char c in fullText)
            {
                newLine.text += c;
                yield return new WaitForSeconds(charInterval);
            }
        }

        #endregion
    }
}
