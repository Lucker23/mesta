// =============================================================================
//  MenuScreens.cs  –  All menu screen management for The Last Ember Knight
// =============================================================================
// Screens managed:
//   Title:         Game logo, "Press Any Key", ambient particles
//   Main Menu:     New Game, Continue, Options, Quit
//   Options:       Music/SFX volume, Resolution, Fullscreen toggle
//   Pause Menu:    Resume, Options, Restart Level, Return to Title
//   Death Screen:  "YOU DIED" + Restart / Return to Title
//   Level Transition: Zone name + level number display
// All screens share ember particle backgrounds and fade transitions.
// =============================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastEmberKnight
{
    public class MenuScreens : MonoBehaviour
    {
        // ─── Singleton ────────────────────────────────────────────────────────────
        public static MenuScreens Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Screen Root Objects

        [Header("Screen Root Objects")]
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup mainMenuGroup;
        [SerializeField] private CanvasGroup optionsGroup;
        [SerializeField] private CanvasGroup pauseGroup;
        [SerializeField] private CanvasGroup deathGroup;
        [SerializeField] private CanvasGroup transitionGroup;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Title Screen

        [Header("Title Screen")]
        [SerializeField] private TextMeshProUGUI titleLogoText;
        [SerializeField] private TextMeshProUGUI pressAnyKeyText;
        [SerializeField] private ParticleSystem  titleParticles;

        private const string LOGO_TEXT       = "THE LAST EMBER KNIGHT";
        private const string PRESS_ANY_KEY   = "PRESS ANY KEY";
        private static readonly Color LOGO_COLOR      = new Color(0.95f, 0.55f, 0.10f, 1f);  // ember orange
        private static readonly Color PRESS_KEY_COLOR = new Color(0.90f, 0.88f, 0.82f, 1f);

        private bool  _waitingForAnyKey = false;
        private float _pressKeyPulse    = 0f;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Main Menu Buttons

        [Header("Main Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button mainMenuOptionsButton;
        [SerializeField] private Button quitButton;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Options Screen

        [Header("Options")]
        [SerializeField] private Slider          musicVolumeSlider;
        [SerializeField] private Slider          sfxVolumeSlider;
        [SerializeField] private TMP_Dropdown    resolutionDropdown;
        [SerializeField] private Toggle          fullscreenToggle;
        [SerializeField] private TextMeshProUGUI musicVolumeLabel;
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel;
        [SerializeField] private Button          optionsBackButton;

        private Resolution[] _resolutions;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Pause Menu

        [Header("Pause Menu")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseOptionsButton;
        [SerializeField] private Button restartLevelButton;
        [SerializeField] private Button pauseReturnToTitleButton;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Death Screen

        [Header("Death Screen")]
        [SerializeField] private TextMeshProUGUI youDiedText;
        [SerializeField] private Button          deathRestartButton;
        [SerializeField] private Button          deathReturnToTitleButton;

        private static readonly Color YOU_DIED_COLOR = new Color(0.85f, 0.05f, 0.05f, 1f);

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Level Transition

        [Header("Level Transition")]
        [SerializeField] private TextMeshProUGUI zoneNameText;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private float           transitionDisplayTime = 2.5f;

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Inspector: Shared Ember Particles + Fade

        [Header("Shared Background")]
        [SerializeField] private ParticleSystem[] emberBackgrounds;  // one per screen group
        [SerializeField] private float            fadeDuration = 0.4f;

        [Header("Full-screen Fade Overlay")]
        [SerializeField] private CanvasGroup      fadeOverlay;

        #endregion

        // ─── Private State ────────────────────────────────────────────────────────
        private CanvasGroup _previousScreen;
        private bool        _optionsFromPause = false;

        // ─────────────────────────────────────────────────────────────────────────
        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            HideAllGroups();
            WireButtons();
            SetupOptionsSliders();
            SetupResolutionDropdown();
        }

        private void Start()
        {
            // Subscribe to game state changes
            GameManager.OnStateChanged += HandleStateChanged;

            // Initialise title text
            if (titleLogoText  != null) { titleLogoText.text = LOGO_TEXT;     titleLogoText.color = LOGO_COLOR; }
            if (pressAnyKeyText != null) { pressAnyKeyText.text = PRESS_ANY_KEY; pressAnyKeyText.color = PRESS_KEY_COLOR; }
            if (youDiedText    != null) { youDiedText.text = "YOU DIED";         youDiedText.color = YOU_DIED_COLOR; }

            // Show title screen
            ShowGroup(titleGroup);
            StartEmberParticles();
            _waitingForAnyKey = true;
        }

        private void Update()
        {
            if (_waitingForAnyKey)
            {
                PulsePressAnyKey();
                if (AnyKeyPressed())
                    OnPressAnyKey();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region State Change Handler

        private void HandleStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Title:           ShowTitleScreen();       break;
                case GameState.LevelTransition: /* handled externally */ break;
                case GameState.Play:            HideAllGroups();         break;
                case GameState.Pause:           ShowPauseMenu();         break;
                case GameState.Dead:            ShowDeathScreen();       break;
                case GameState.Options:         ShowOptionsScreen();     break;
                case GameState.End:             HideAllGroups();         break;
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Show / Hide Screens

        public void ShowTitleScreen()
        {
            StartCoroutine(TransitionTo(titleGroup));
            _waitingForAnyKey = true;
            StartEmberParticles();
        }

        public void ShowMainMenu()
        {
            _waitingForAnyKey = false;

            // Show Continue only if a save exists
            bool hasSave = PlayerPrefs.HasKey(GameConstants.SaveKeyLevel);
            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);

            StartCoroutine(TransitionTo(mainMenuGroup));
        }

        public void ShowOptionsScreen()
        {
            LoadOptionsValues();
            StartCoroutine(TransitionTo(optionsGroup));
        }

        public void ShowPauseMenu()
        {
            StartCoroutine(TransitionTo(pauseGroup));
        }

        public void ShowDeathScreen()
        {
            StartCoroutine(TransitionTo(deathGroup));
        }

        public void ShowLevelTransition(ZoneType zone, int levelIndex)
        {
            if (zoneNameText  != null) zoneNameText.text  = GetZoneDisplayName(zone).ToUpper();
            if (levelNumberText != null) levelNumberText.text = "LEVEL " + (levelIndex + 1).ToString();

            StartCoroutine(LevelTransitionSequence());
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Button Actions

        private void WireButtons()
        {
            if (newGameButton             != null) newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton            != null) continueButton.onClick.AddListener(OnContinue);
            if (mainMenuOptionsButton     != null) mainMenuOptionsButton.onClick.AddListener(OnOpenOptionsFromMenu);
            if (quitButton                != null) quitButton.onClick.AddListener(OnQuit);

            if (resumeButton              != null) resumeButton.onClick.AddListener(OnResume);
            if (pauseOptionsButton        != null) pauseOptionsButton.onClick.AddListener(OnOpenOptionsFromPause);
            if (restartLevelButton        != null) restartLevelButton.onClick.AddListener(OnRestartLevel);
            if (pauseReturnToTitleButton  != null) pauseReturnToTitleButton.onClick.AddListener(OnReturnToTitle);

            if (deathRestartButton        != null) deathRestartButton.onClick.AddListener(OnRestartLevel);
            if (deathReturnToTitleButton  != null) deathReturnToTitleButton.onClick.AddListener(OnReturnToTitle);

            if (optionsBackButton         != null) optionsBackButton.onClick.AddListener(OnOptionsBack);
        }

        private void OnPressAnyKey()
        {
            _waitingForAnyKey = false;
            ShowMainMenu();
        }

        private void OnNewGame()
        {
            StartCoroutine(FadeAndAction(fadeDuration, () => GameManager.Instance?.StartGame()));
        }

        private void OnContinue()
        {
            int savedLevel = PlayerPrefs.GetInt(GameConstants.SaveKeyLevel, 0);
            StartCoroutine(FadeAndAction(fadeDuration, () => GameManager.Instance?.LoadLevel(savedLevel)));
        }

        private void OnOpenOptionsFromMenu()
        {
            _optionsFromPause = false;
            ShowOptionsScreen();
        }

        private void OnOpenOptionsFromPause()
        {
            _optionsFromPause = true;
            ShowOptionsScreen();
        }

        private void OnOptionsBack()
        {
            SaveOptionsValues();
            if (_optionsFromPause) ShowPauseMenu();
            else                   ShowMainMenu();
        }

        private void OnResume()  => GameManager.Instance?.ResumeGame();
        private void OnRestartLevel() =>
            StartCoroutine(FadeAndAction(fadeDuration, () => GameManager.Instance?.RestartFromDeath()));
        private void OnReturnToTitle() =>
            StartCoroutine(FadeAndAction(fadeDuration, () => GameManager.Instance?.ReturnToTitle()));
        private void OnQuit() =>
            StartCoroutine(FadeAndAction(fadeDuration, Application.Quit));

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Options Logic

        private void SetupOptionsSliders()
        {
            if (musicVolumeSlider != null) { musicVolumeSlider.minValue = 0f; musicVolumeSlider.maxValue = 1f; musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged); }
            if (sfxVolumeSlider   != null) { sfxVolumeSlider.minValue   = 0f; sfxVolumeSlider.maxValue   = 1f; sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);   }
            if (fullscreenToggle  != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        private void SetupResolutionDropdown()
        {
            if (resolutionDropdown == null) return;
            _resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            int currentIndex = 0;
            var options = new System.Collections.Generic.List<string>();
            for (int i = 0; i < _resolutions.Length; i++)
            {
                options.Add($"{_resolutions[i].width} x {_resolutions[i].height}");
                if (_resolutions[i].width  == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                    currentIndex = i;
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentIndex;
            resolutionDropdown.RefreshShownValue();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        private void LoadOptionsValues()
        {
            if (musicVolumeSlider != null) musicVolumeSlider.value = PlayerPrefs.GetFloat("opt_music", 0.8f);
            if (sfxVolumeSlider   != null) sfxVolumeSlider.value   = PlayerPrefs.GetFloat("opt_sfx",   0.9f);
            if (fullscreenToggle  != null) fullscreenToggle.isOn   = PlayerPrefs.GetInt("opt_fullscreen", 1) == 1;
        }

        private void SaveOptionsValues()
        {
            if (musicVolumeSlider != null) PlayerPrefs.SetFloat("opt_music", musicVolumeSlider.value);
            if (sfxVolumeSlider   != null) PlayerPrefs.SetFloat("opt_sfx",   sfxVolumeSlider.value);
            if (fullscreenToggle  != null) PlayerPrefs.SetInt("opt_fullscreen", fullscreenToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (musicVolumeLabel != null) musicVolumeLabel.text = Mathf.RoundToInt(value * 100f) + "%";
            // Hook into AudioManager when available: AudioManager.Instance?.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (sfxVolumeLabel != null) sfxVolumeLabel.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void OnFullscreenToggled(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        private void OnResolutionChanged(int index)
        {
            if (_resolutions == null || index >= _resolutions.Length) return;
            Resolution res = _resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────────
        #region Transitions and Helpers

        private void HideAllGroups()
        {
            SetGroupVisible(titleGroup,      false);
            SetGroupVisible(mainMenuGroup,   false);
            SetGroupVisible(optionsGroup,    false);
            SetGroupVisible(pauseGroup,      false);
            SetGroupVisible(deathGroup,      false);
            SetGroupVisible(transitionGroup, false);
        }

        private static void SetGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha            = visible ? 1f : 0f;
            group.interactable     = visible;
            group.blocksRaycasts   = visible;
        }

        private void ShowGroup(CanvasGroup group)
        {
            SetGroupVisible(group, true);
        }

        private IEnumerator TransitionTo(CanvasGroup target)
        {
            // Fade out current
            if (_previousScreen != null)
            {
                yield return StartCoroutine(FadeGroup(_previousScreen, 1f, 0f, fadeDuration * 0.5f));
                SetGroupVisible(_previousScreen, false);
            }

            SetGroupVisible(target, false);
            target.alpha = 0f;
            target.interactable   = true;
            target.blocksRaycasts = true;

            yield return StartCoroutine(FadeGroup(target, 0f, 1f, fadeDuration));
            _previousScreen = target;
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

        private IEnumerator FadeAndAction(float duration, System.Action action)
        {
            if (fadeOverlay != null)
            {
                yield return StartCoroutine(FadeGroup(fadeOverlay, 0f, 1f, duration));
            }
            action?.Invoke();
        }

        private IEnumerator LevelTransitionSequence()
        {
            SetGroupVisible(transitionGroup, false);
            transitionGroup.alpha = 0f;

            yield return StartCoroutine(FadeGroup(transitionGroup, 0f, 1f, 0.5f));
            yield return new WaitForSeconds(transitionDisplayTime);
            yield return StartCoroutine(FadeGroup(transitionGroup, 1f, 0f, 0.5f));

            SetGroupVisible(transitionGroup, false);
            GameManager.Instance?.FinishLevelTransition();
        }

        private void StartEmberParticles()
        {
            if (emberBackgrounds == null) return;
            foreach (ParticleSystem ps in emberBackgrounds)
                if (ps != null) ps.Play();
        }

        private void PulsePressAnyKey()
        {
            if (pressAnyKeyText == null) return;
            _pressKeyPulse += Time.deltaTime * 2.0f;
            float alpha = (Mathf.Sin(_pressKeyPulse) + 1f) * 0.5f;
            Color c = pressAnyKeyText.color; c.a = Mathf.Lerp(0.3f, 1f, alpha);
            pressAnyKeyText.color = c;
        }

        private static bool AnyKeyPressed()
        {
            return Input.anyKeyDown;
        }

        private static string GetZoneDisplayName(ZoneType zone)
        {
            return zone switch
            {
                ZoneType.Ashfields        => "Ashfields",
                ZoneType.EmberCrypts      => "Ember Crypts",
                ZoneType.ShatteredRamparts => "Shattered Ramparts",
                ZoneType.SlagPits         => "Slag Pits",
                ZoneType.DrownedCitadel   => "Drowned Citadel",
                ZoneType.BoneWastes       => "Bone Wastes",
                ZoneType.ObsidianSpire    => "Obsidian Spire",
                ZoneType.TheWound         => "The Wound",
                ZoneType.DragonsApproach  => "Dragon's Approach",
                ZoneType.ThroneOfAsh      => "Throne of Ash",
                _                          => "Unknown Zone"
            };
        }

        #endregion
    }
}
