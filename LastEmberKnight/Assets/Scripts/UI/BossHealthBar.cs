// =============================================================================
//  BossHealthBar.cs  –  Bottom-of-screen boss HP bar for The Last Ember Knight
// =============================================================================
// Features:
//   - Large red HP fill bar with ghost bar that lags behind (smooth damage drain)
//   - Boss name in dramatic all-caps TextMeshPro
//   - Phase indicator dots (dim past, bright active, faded future)
//   - Fade-in on boss enter, fade-out on boss death
//   - Special Easter egg: Elder Titan (Boss1) shows "Easter egg by Lucker23"
//     in small subtitle text beneath the name
// =============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    public class BossHealthBar : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static BossHealthBar Instance { get; private set; }

        // ─── Inspector ────────────────────────────────────────────────────────────
        [Header("Bar References")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Slider      hpSlider;
        [SerializeField] private Slider      ghostSlider;
        [SerializeField] private Image       hpFill;
        [SerializeField] private Image       ghostFill;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TextMeshProUGUI easterEggSubtitle;

        [Header("Phase Dots")]
        [SerializeField] private GameObject phaseDotPrefab;
        [SerializeField] private Transform  phaseDotParent;

        // ─── Colors ───────────────────────────────────────────────────────────────
        private static readonly Color HP_BAR_COLOR    = new Color(0.85f, 0.10f, 0.10f, 1f);
        private static readonly Color GHOST_BAR_COLOR = new Color(0.65f, 0.55f, 0.10f, 0.70f);
        private static readonly Color DOT_ACTIVE      = Color.white;
        private static readonly Color DOT_PAST        = new Color(0.30f, 0.30f, 0.30f, 1f);
        private static readonly Color DOT_FUTURE      = new Color(0.70f, 0.70f, 0.70f, 1f);

        // ─── Constants ────────────────────────────────────────────────────────────
        private const float GHOST_LAG_DELAY      = 0.6f;   // seconds before ghost catches up
        private const float GHOST_LERP_SPEED     = 2.5f;
        private const float HP_LERP_SPEED        = 8f;
        private const float APPEAR_FADE_TIME     = 0.5f;
        private const float DISAPPEAR_FADE_TIME  = 1.0f;

        // Elder Titan Easter egg
        private const string EASTER_EGG_BOSS_ID  = "Boss1ElderTitan";
        private const string EASTER_EGG_TEXT      = "Easter egg by Lucker23";

        // ─── Runtime State ────────────────────────────────────────────────────────
        private float   _targetHP        = 1f;
        private float   _displayedHP     = 1f;
        private float   _ghostHP         = 1f;
        private float   _ghostDelayTimer = 0f;
        private int     _totalPhases     = 1;
        private int     _currentPhase    = 1;
        private Image[] _phaseDots;
        private bool    _isVisible       = false;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (hpFill    != null) hpFill.color    = HP_BAR_COLOR;
            if (ghostFill != null) ghostFill.color  = GHOST_BAR_COLOR;

            // Start invisible
            if (rootGroup != null) { rootGroup.alpha = 0f; rootGroup.interactable = false; rootGroup.blocksRaycasts = false; }
            if (easterEggSubtitle != null) easterEggSubtitle.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isVisible) return;
            InterpolateBars();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Public API (called by BossBase)

        /// <summary>
        /// Initialise and fade-in the boss health bar.
        /// Called from BossBase.Start().
        /// </summary>
        public void Initialise(string displayName, float maxHP, bool isElderTitan)
        {
            _totalPhases   = 2; // all bosses have 2 phases
            _currentPhase  = 1;
            _targetHP      = 1f;
            _displayedHP   = 1f;
            _ghostHP       = 1f;
            _ghostDelayTimer = 0f;

            string bossId = isElderTitan ? EASTER_EGG_BOSS_ID : displayName;
            ConfigureName(bossId, displayName);
            BuildPhaseDots(_totalPhases);
            RefreshPhaseDots();

            StopAllCoroutines();
            StartCoroutine(FadeRoot(0f, 1f, APPEAR_FADE_TIME));
            _isVisible = true;
        }

        /// <summary>Push a new HP target. current/max are raw floats.</summary>
        public void SetHP(float current, float max)
        {
            if (max <= 0f) return;
            float newTarget = Mathf.Clamp01(current / max);
            if (newTarget < _targetHP)
                _ghostDelayTimer = GHOST_LAG_DELAY;   // reset ghost lag when taking damage
            _targetHP = newTarget;
        }

        /// <summary>Advance phase indicator. phase is 1-based.</summary>
        public void SetPhase(int phase)
        {
            _currentPhase = Mathf.Clamp(phase, 1, _totalPhases);
            RefreshPhaseDots();
        }

        /// <summary>Fade out and hide the bar (boss defeated / left scene).</summary>
        public void Hide()
        {
            _isVisible = false;
            StopAllCoroutines();
            float from = rootGroup != null ? rootGroup.alpha : 1f;
            StartCoroutine(FadeRoot(from, 0f, DISAPPEAR_FADE_TIME));
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Private Helpers

        private void ConfigureName(string bossId, string displayName)
        {
            if (bossNameText != null)
            {
                bossNameText.text      = displayName.ToUpper();
                bossNameText.fontStyle = FontStyles.Bold;
            }

            bool isEasterEgg = (bossId == EASTER_EGG_BOSS_ID);
            if (easterEggSubtitle != null)
            {
                easterEggSubtitle.gameObject.SetActive(isEasterEgg);
                if (isEasterEgg)
                    easterEggSubtitle.text = EASTER_EGG_TEXT;
            }
        }

        private void BuildPhaseDots(int count)
        {
            // Destroy old dots
            if (phaseDotParent != null)
                foreach (Transform child in phaseDotParent)
                    Destroy(child.gameObject);

            if (phaseDotPrefab == null || phaseDotParent == null) return;

            _phaseDots = new Image[count];
            for (int i = 0; i < count; i++)
            {
                GameObject dot = Instantiate(phaseDotPrefab, phaseDotParent);
                _phaseDots[i]  = dot.GetComponent<Image>();
            }
        }

        private void RefreshPhaseDots()
        {
            if (_phaseDots == null) return;
            for (int i = 0; i < _phaseDots.Length; i++)
            {
                if (_phaseDots[i] == null) continue;
                bool past   = (i + 1) < _currentPhase;
                bool active = (i + 1) == _currentPhase;
                _phaseDots[i].color = active ? DOT_ACTIVE : past ? DOT_PAST : DOT_FUTURE;
            }
        }

        private void InterpolateBars()
        {
            float dt = Time.deltaTime;

            // Main HP bar: fast lerp
            _displayedHP = Mathf.Lerp(_displayedHP, _targetHP, dt * HP_LERP_SPEED);
            if (hpSlider != null) hpSlider.value = _displayedHP;

            // Ghost bar: delayed, then slow catch-up
            if (_ghostDelayTimer > 0f)
                _ghostDelayTimer -= dt;
            else
                _ghostHP = Mathf.Lerp(_ghostHP, _targetHP, dt * GHOST_LERP_SPEED);

            if (ghostSlider != null) ghostSlider.value = _ghostHP;
        }

        private IEnumerator FadeRoot(float from, float to, float duration)
        {
            if (rootGroup == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rootGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            rootGroup.alpha = to;
        }

        #endregion
    }
}
