// =============================================================================
//  EndingSequence.cs  –  Level 50 completion ending and Lucker23 post-credits
// =============================================================================
// Triggered after level 49 (final boss defeated).
//
// Phase 1 (0-12 s)  – Story credits:
//   "The empire is ash."     – fade in/out 2 s each
//   "The knight is ash."
//   "But from ash…"
//   "all things begin again."
//   "THE END"
//
// Phase 2 (12 s+)  – Lucker23 post-credits (CRT terminal aesthetic):
//   Types out:
//     "Hey… you actually beat it. Respect."
//     "That Elder Titan swap was my doing btw"
//     "If you liked this game…"
//     "Come drop feedback on Discord!"
//     "Find NONEK there — he built this whole thing."
//     "I just added some spice."
//   Purple Discord CTA box
//   "Tell him Lucker sent you"
//   "Press any key to return"
//   → on any key: Return to Title
// =============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    public class EndingSequence : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static EndingSequence Instance { get; private set; }

        // ─── Inspector: Story Text ────────────────────────────────────────────────
        [Header("Story Phase")]
        [SerializeField] private CanvasGroup     storyGroup;
        [SerializeField] private TextMeshProUGUI storyLine;
        [SerializeField] private float           lineFadeTime = 1.0f;
        [SerializeField] private float           lineHoldTime = 1.5f;

        // ─── Inspector: Post-credits ──────────────────────────────────────────────
        [Header("Post-Credits (Lucker23)")]
        [SerializeField] private CanvasGroup     postCreditsGroup;
        [SerializeField] private TextMeshProUGUI terminalText;
        [SerializeField] private Image           discordBox;
        [SerializeField] private TextMeshProUGUI discordBoxText;
        [SerializeField] private TextMeshProUGUI pressAnyKeyText;
        [SerializeField] private CanvasGroup     postCreditsRoot;

        // ─── Inspector: Shared ────────────────────────────────────────────────────
        [Header("Shared")]
        [SerializeField] private CanvasGroup     blackFadeOverlay;

        // ─── Colors ───────────────────────────────────────────────────────────────
        private static readonly Color STORY_TEXT_COLOR   = new Color(0.92f, 0.88f, 0.80f);
        private static readonly Color THE_END_COLOR      = new Color(0.95f, 0.55f, 0.10f);
        private static readonly Color TERMINAL_GREEN     = new Color(0.15f, 1.00f, 0.20f);
        private static readonly Color DISCORD_PURPLE     = new Color(0.44f, 0.42f, 0.85f, 0.90f);

        // ─── Typing Speed ─────────────────────────────────────────────────────────
        private const float CHARS_PER_SECOND = 24f;

        // ─── Story Lines ──────────────────────────────────────────────────────────
        private static readonly string[] StoryLines = new string[]
        {
            "The empire is ash.",
            "The knight is ash.",
            "But from ash\u2026",
            "all things begin again.",
            "THE END"
        };

        // ─── Lucker23 Terminal Lines ──────────────────────────────────────────────
        private static readonly string[] LuckerLines = new string[]
        {
            "Hey\u2026 you actually beat it. Respect.",
            "That Elder Titan swap was my doing btw",
            "If you liked this game\u2026",
            "Come drop feedback on Discord!",
            "Find NONEK there \u2014 he built this whole thing.",
            "I just added some spice.",
        };

        // ─── State ────────────────────────────────────────────────────────────────
        private bool _waitingForAnyKey = false;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            HideAll();
        }

        private void Start()
        {
            // Auto-start if this scene was loaded as the end scene
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentGameState == GameState.End)
            {
                StartEnding();
            }
        }

        private void Update()
        {
            if (_waitingForAnyKey && Input.anyKeyDown)
            {
                _waitingForAnyKey = false;
                GameManager.Instance?.ReturnToTitle();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>Begin the full ending sequence (story + Lucker23 post-credits).</summary>
        public void StartEnding()
        {
            StartCoroutine(FullEndingSequence());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Sequence Coroutines

        private IEnumerator FullEndingSequence()
        {
            // Fade in from black
            yield return FadeGroup(blackFadeOverlay, 1f, 0f, 1.0f);

            // Phase 1: Story lines
            yield return StoryPhase();

            // Brief pause then fade to black before Lucker post-credits
            yield return FadeGroup(blackFadeOverlay, 0f, 1f, 0.8f);
            yield return new WaitForSeconds(0.5f);

            // Phase 2: Lucker23 post-credits
            yield return PostCreditsPhase();
        }

        // ── Phase 1 ───────────────────────────────────────────────────────────────
        private IEnumerator StoryPhase()
        {
            if (storyGroup != null)
            {
                storyGroup.alpha            = 1f;
                storyGroup.interactable     = false;
                storyGroup.blocksRaycasts   = false;
            }

            for (int i = 0; i < StoryLines.Length; i++)
            {
                string line = StoryLines[i];
                bool isTheEnd = (i == StoryLines.Length - 1);

                if (storyLine != null)
                {
                    storyLine.text  = line;
                    storyLine.color = isTheEnd
                        ? new Color(THE_END_COLOR.r, THE_END_COLOR.g, THE_END_COLOR.b, 0f)
                        : new Color(STORY_TEXT_COLOR.r, STORY_TEXT_COLOR.g, STORY_TEXT_COLOR.b, 0f);
                    storyLine.fontStyle = isTheEnd ? FontStyles.Bold : FontStyles.Normal;
                    storyLine.fontSize  = isTheEnd ? 64f : 40f;
                }

                // Fade in
                yield return FadeTextAlpha(storyLine, 0f, 1f, lineFadeTime);
                yield return new WaitForSeconds(lineHoldTime);
                // Fade out (except THE END – hold a beat longer)
                float holdExtra = isTheEnd ? 1.0f : 0f;
                yield return new WaitForSeconds(holdExtra);
                yield return FadeTextAlpha(storyLine, 1f, 0f, lineFadeTime);
            }

            if (storyGroup != null) storyGroup.alpha = 0f;
        }

        // ── Phase 2 ───────────────────────────────────────────────────────────────
        private IEnumerator PostCreditsPhase()
        {
            // Show post-credits group
            if (postCreditsRoot != null)
            {
                postCreditsRoot.alpha          = 1f;
                postCreditsRoot.interactable   = false;
                postCreditsRoot.blocksRaycasts = false;
            }

            // Clear terminal
            if (terminalText != null)
            {
                terminalText.text  = "";
                terminalText.color = TERMINAL_GREEN;
            }

            // Hide Discord box and press-any-key until we're ready
            SetGroupAlpha(discordBox,      0f);
            SetGroupAlpha(pressAnyKeyText, 0f);

            // Enable HackOverlay for CRT aesthetic
            HackOverlay overlay = HackOverlay.Instance;
            if (overlay != null)
                overlay.EnableOverlay(new Color(0.05f, 0.05f, 0.15f), 0.30f);

            // Fade in from black
            yield return FadeGroup(blackFadeOverlay, 1f, 0f, 1.0f);

            // Type each Lucker line
            foreach (string line in LuckerLines)
            {
                yield return TypeLine(line, 0.4f);
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(0.5f);

            // Show Discord CTA box
            if (discordBox != null)
            {
                discordBox.color = DISCORD_PURPLE;
                discordBox.gameObject.SetActive(true);
            }
            if (discordBoxText != null)
            {
                discordBoxText.text  = "Tell him Lucker sent you";
                discordBoxText.color = Color.white;
            }

            yield return FadeGraphicAlpha(discordBox, 0f, 1f, 0.6f);

            yield return new WaitForSeconds(1.5f);

            // Press any key prompt
            if (pressAnyKeyText != null)
            {
                pressAnyKeyText.text  = "Press any key to return";
                pressAnyKeyText.color = new Color(0.85f, 0.85f, 0.85f, 0f);
                pressAnyKeyText.gameObject.SetActive(true);
            }
            yield return FadeTextAlpha(pressAnyKeyText, 0f, 1f, 0.5f);

            // Start pulsing the prompt
            StartCoroutine(PulseText(pressAnyKeyText));
            _waitingForAnyKey = true;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Helpers

        private IEnumerator TypeLine(string text, float predelay = 0f)
        {
            if (predelay > 0f) yield return new WaitForSeconds(predelay);
            if (terminalText == null) yield break;

            float charInterval = 1f / CHARS_PER_SECOND;
            string current = terminalText.text;
            if (current.Length > 0) current += "\n";

            foreach (char c in text)
            {
                current += c;
                terminalText.text = current;
                yield return new WaitForSeconds(charInterval);
            }
        }

        private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private static IEnumerator FadeTextAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
        {
            if (tmp == null) yield break;
            float elapsed = 0f;
            Color c = tmp.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, elapsed / duration);
                tmp.color = c;
                yield return null;
            }
            c.a = to; tmp.color = c;
        }

        private static IEnumerator FadeGraphicAlpha(Graphic graphic, float from, float to, float duration)
        {
            if (graphic == null) yield break;
            float elapsed = 0f;
            Color c = graphic.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, elapsed / duration);
                graphic.color = c;
                yield return null;
            }
            c.a = to; graphic.color = c;
        }

        private static IEnumerator PulseText(TextMeshProUGUI tmp)
        {
            if (tmp == null) yield break;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * 1.8f;
                float alpha = (Mathf.Sin(t) + 1f) * 0.5f;
                alpha = Mathf.Lerp(0.3f, 1f, alpha);
                Color c = tmp.color; c.a = alpha; tmp.color = c;
                yield return null;
            }
        }

        private static void SetGroupAlpha(Component c, float alpha)
        {
            if (c == null) return;
            CanvasGroup cg = c.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = alpha; return; }
            Graphic g = c.GetComponent<Graphic>();
            if (g != null) { Color col = g.color; col.a = alpha; g.color = col; }
        }

        private void HideAll()
        {
            SetGroupAlpha(storyGroup,       0f);
            SetGroupAlpha(postCreditsRoot,  0f);
            if (discordBox      != null) discordBox.gameObject.SetActive(false);
            if (pressAnyKeyText != null) pressAnyKeyText.gameObject.SetActive(false);
            if (blackFadeOverlay != null) blackFadeOverlay.alpha = 1f;
        }

        #endregion
    }
}
