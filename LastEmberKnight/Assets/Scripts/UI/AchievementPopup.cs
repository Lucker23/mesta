// =============================================================================
//  AchievementPopup.cs  –  Sliding achievement notification system
// =============================================================================
// Shows a notification panel sliding in from the bottom-right corner:
//   - Slides in from right → waits 3s → slides back out
//   - Achievement icon (colored shape) + name + description text
//   - Queue system: multiple achievements display one at a time
//   - Golden border with alpha pulse glow
// =============================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    public class AchievementPopup : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static AchievementPopup Instance { get; private set; }

        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Panel References")]
        [SerializeField] private RectTransform  panelRect;
        [SerializeField] private CanvasGroup    panelGroup;
        [SerializeField] private Image          achievementIcon;
        [SerializeField] private TextMeshProUGUI achievementName;
        [SerializeField] private TextMeshProUGUI achievementDesc;
        [SerializeField] private Image          borderImage;

        [Header("Icon Library")]
        [SerializeField] private Sprite[] iconSprites;
        [SerializeField] private Color[]  iconColors;

        [Header("Animation")]
        [SerializeField] private float slideDistance = 420f;   // pixels off-screen right
        [SerializeField] private float slideInTime   = 0.40f;
        [SerializeField] private float holdTime      = 3.00f;
        [SerializeField] private float slideOutTime  = 0.35f;

        // ─── Golden border glow ───────────────────────────────────────────────────
        private static readonly Color BORDER_GOLD  = new Color(1f, 0.82f, 0.10f, 1f);
        private const float GLOW_PULSE_SPEED        = 3.0f;
        private const float GLOW_MIN                = 0.55f;
        private const float GLOW_MAX                = 1.00f;

        // ─── Queue / State ────────────────────────────────────────────────────────
        private readonly Queue<AchievementEntry> _queue = new Queue<AchievementEntry>();
        private bool    _isShowing       = false;
        private Vector2 _visiblePos;
        private Vector2 _hiddenPos;
        private bool    _posInitialised  = false;

        // ─────────────────────────────────────────────────────────────────────────
        #region Data

        private readonly struct AchievementEntry
        {
            public readonly string Name;
            public readonly string Description;
            public readonly int    IconIndex;
            public AchievementEntry(string name, string desc, int icon)
            {
                Name        = name;
                Description = desc;
                IconIndex   = icon;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (borderImage != null) borderImage.color = BORDER_GOLD;
            if (panelGroup  != null) { panelGroup.alpha = 0f; panelGroup.interactable = false; panelGroup.blocksRaycasts = false; }
        }

        private void Start()
        {
            CachePositions();
        }

        private void Update()
        {
            if (_isShowing) PulseBorderGlow();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API

        /// <summary>
        /// Queue an achievement notification.
        /// iconIndex maps to the iconSprites / iconColors arrays.
        /// </summary>
        public void ShowAchievement(string name, string description, int iconIndex = 0)
        {
            _queue.Enqueue(new AchievementEntry(name, description, iconIndex));
            if (!_isShowing)
                StartCoroutine(ProcessQueue());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Private Helpers

        private void CachePositions()
        {
            if (_posInitialised || panelRect == null) return;
            _visiblePos = panelRect.anchoredPosition;
            _hiddenPos  = _visiblePos + new Vector2(slideDistance, 0f);
            panelRect.anchoredPosition = _hiddenPos;
            _posInitialised = true;
        }

        private void PopulatePanel(in AchievementEntry entry)
        {
            if (achievementName != null) achievementName.text = entry.Name;
            if (achievementDesc != null) achievementDesc.text = entry.Description;

            if (achievementIcon != null)
            {
                int idx = Mathf.Clamp(entry.IconIndex, 0,
                    (iconSprites != null ? iconSprites.Length : 1) - 1);

                if (iconSprites != null && idx < iconSprites.Length)
                    achievementIcon.sprite = iconSprites[idx];

                achievementIcon.color = (iconColors != null && idx < iconColors.Length)
                    ? iconColors[idx]
                    : BORDER_GOLD;
            }
        }

        private IEnumerator ProcessQueue()
        {
            CachePositions();
            _isShowing = true;

            while (_queue.Count > 0)
            {
                AchievementEntry entry = _queue.Dequeue();
                PopulatePanel(in entry);

                yield return StartCoroutine(SlideIn());
                yield return new WaitForSeconds(holdTime);
                yield return StartCoroutine(SlideOut());
            }

            _isShowing = false;
        }

        private IEnumerator SlideIn()
        {
            if (panelRect == null) yield break;
            panelRect.anchoredPosition = _hiddenPos;
            if (panelGroup != null) { panelGroup.alpha = 1f; panelGroup.blocksRaycasts = true; }

            float elapsed = 0f;
            while (elapsed < slideInTime)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep(elapsed / slideInTime);
                panelRect.anchoredPosition = Vector2.Lerp(_hiddenPos, _visiblePos, t);
                yield return null;
            }
            panelRect.anchoredPosition = _visiblePos;
        }

        private IEnumerator SlideOut()
        {
            if (panelRect == null) yield break;

            float elapsed = 0f;
            while (elapsed < slideOutTime)
            {
                elapsed += Time.deltaTime;
                float t = SmoothStep(elapsed / slideOutTime);
                panelRect.anchoredPosition = Vector2.Lerp(_visiblePos, _hiddenPos, t);
                if (panelGroup != null) panelGroup.alpha = 1f - t;
                yield return null;
            }
            panelRect.anchoredPosition = _hiddenPos;
            if (panelGroup != null) { panelGroup.alpha = 0f; panelGroup.blocksRaycasts = false; }
        }

        private void PulseBorderGlow()
        {
            if (borderImage == null) return;
            float a = Mathf.Lerp(GLOW_MIN, GLOW_MAX,
                          (Mathf.Sin(Time.time * GLOW_PULSE_SPEED) + 1f) * 0.5f);
            Color c = borderImage.color; c.a = a;
            borderImage.color = c;
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        #endregion
    }
}
