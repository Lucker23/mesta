// =============================================================================
//  GameManager.cs  –  Singleton orchestrating the entire game-state machine
// =============================================================================
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastEmberKnight
{
    /// <summary>
    /// Central singleton that owns the top-level <see cref="StateMachine{T}"/> and
    /// coordinates all major game-flow transitions (title, level load, death, pause,
    /// shrine, end-game).
    ///
    /// Lifecycle:
    ///   Awake  – enforce singleton, build state machine
    ///   Start  – enter Title state
    ///   Update – tick state machine
    ///
    /// Level layout (0-based indices 0-49):
    ///   Zone  = Mathf.Clamp(level / 5, 0, 9)
    ///   Boss  = (level % 5 == 4)   → levels 4, 9, 14, 19, 24, 29, 34, 39, 44, 49
    ///   Difficulty = 1 + level * 0.12
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        // =====================================================================
        //  Singleton
        // =====================================================================

        /// <summary>Global, always-valid reference to the single GameManager instance.</summary>
        public static GameManager Instance { get; private set; }

        // =====================================================================
        //  Inspector configuration
        // =====================================================================

        [Header("Scene Names")]
        [SerializeField] private string titleSceneName       = GameConstants.SceneTitle;
        [SerializeField] private string gameSceneName        = GameConstants.SceneGame;
        [SerializeField] private string endSceneName         = GameConstants.SceneCredits;

        [Header("Starting Level (0-based)")]
        [SerializeField] private int    startingLevel        = 0;

        // =====================================================================
        //  Private runtime state
        // =====================================================================

        private int      _currentLevel;
        private ZoneType _currentZone;
        private float    _difficulty;
        private int      _totalShards;
        private bool     _isGameOver;

        // Backing state machine
        private StateMachine<GameState> _stateMachine;

        // =====================================================================
        //  Public properties
        // =====================================================================

        /// <summary>0-based index of the level currently loaded (or being transitioned to).</summary>
        public int      Level       => _currentLevel;

        /// <summary>Zone derived from the current level: floor(level / 5), capped at 9.</summary>
        public ZoneType Zone        => _currentZone;

        /// <summary>Difficulty scalar: 1 + level * 0.12.</summary>
        public float    Difficulty  => _difficulty;

        /// <summary>Total ember shards collected across the current run.</summary>
        public int TotalShards
        {
            get => _totalShards;
            set => _totalShards = Mathf.Max(0, value);
        }

        /// <summary>True while the player is in the Dead state (cleared on restart).</summary>
        public bool IsGameOver => _isGameOver;

        /// <summary>Current state machine key; safe to read from any system.</summary>
        public GameState CurrentGameState => _stateMachine?.CurrentKey ?? GameState.Title;

        // =====================================================================
        //  Static events  (fire-and-forget; UI / audio / analytics subscribe)
        // =====================================================================

        /// <summary>Fired whenever the state machine transitions. Passes the new state.</summary>
        public static event Action<GameState> OnStateChanged;

        /// <summary>Fired immediately after level data is committed and the scene begins loading.</summary>
        public static event Action<int>       OnLevelLoaded;

        /// <summary>Fired when <see cref="TriggerPlayerDeath"/> moves into the Dead state.</summary>
        public static event Action            OnPlayerDied;

        /// <summary>
        /// Fired when <see cref="NotifyBossDefeated"/> is called.
        /// Passes the 0-based level index of the defeated boss.
        /// </summary>
        public static event Action<int>       OnBossDefeated;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            // --- Enforce singleton ---
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildStateMachine();
        }

        private void Start()
        {
            _stateMachine.SetInitialState(GameState.Title);
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                if (_stateMachine != null)
                    _stateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        // =====================================================================
        //  State machine construction
        // =====================================================================

        private void BuildStateMachine()
        {
            _stateMachine = new StateMachine<GameState>();

            _stateMachine.AddState(GameState.Title,           new TitleState(this));
            _stateMachine.AddState(GameState.LevelTransition, new LevelTransitionState(this));
            _stateMachine.AddState(GameState.Play,            new PlayState(this));
            _stateMachine.AddState(GameState.Shrine,          new ShrineState(this));
            _stateMachine.AddState(GameState.Dead,            new DeadState(this));
            _stateMachine.AddState(GameState.Pause,           new PauseState(this));
            _stateMachine.AddState(GameState.Options,         new OptionsState(this));
            _stateMachine.AddState(GameState.End,             new EndState(this));

            _stateMachine.OnStateChanged += HandleStateChanged;
        }

        // Relay the internal (from, to) event to the public single-arg event.
        private void HandleStateChanged(GameState from, GameState to)
        {
            OnStateChanged?.Invoke(to);
        }

        // =====================================================================
        //  Public API – game-flow methods
        // =====================================================================

        /// <summary>
        /// Begin a fresh run from level 0.  Called by the "New Game" UI button.
        /// </summary>
        public void StartGame()
        {
            _isGameOver  = false;
            _totalShards = 0;
            LoadLevel(startingLevel);
        }

        /// <summary>
        /// Load an arbitrary level by its 0-based index.
        /// Transitions → LevelTransition state, updates zone/difficulty,
        /// triggers <see cref="OnLevelLoaded"/>, and asks the level pipeline to prepare content.
        /// </summary>
        /// <param name="level">0-based level index [0, 49].</param>
        public void LoadLevel(int level)
        {
            if (level < 0 || level >= GameConstants.TotalLevels)
            {
                Debug.LogWarning($"[GameManager] LoadLevel: index {level} is out of range [0, {GameConstants.TotalLevels - 1}].");
                return;
            }

            _currentLevel = level;
            _currentZone  = CalculateZone(level);
            _difficulty   = CalculateDifficulty(level);

            // Move state machine first so any listeners get the transition event
            _stateMachine.ChangeState(GameState.LevelTransition);

            // Persist progress before the scene reload clears managed-object refs
            SaveSystem.SaveGame(BuildSaveData());

            // Prepare level data (room layout, enemy list, boss flag)
            LevelGenerator.PrepareLevel(level, _currentZone, IsBossLevel(level));

            // Load scene asynchronously if desired; synchronous for simplicity here
            SceneManager.LoadScene(gameSceneName);

            OnLevelLoaded?.Invoke(level);
        }

        /// <summary>
        /// Advance to the next level, or trigger the End state if all 50 are complete.
        /// </summary>
        public void NextLevel()
        {
            int next = _currentLevel + 1;

            if (next >= GameConstants.TotalLevels)
            {
                _stateMachine.ChangeState(GameState.End);
                SceneManager.LoadScene(endSceneName);
                return;
            }

            LoadLevel(next);
        }

        /// <summary>
        /// Called by the boss's defeat hook; fires <see cref="OnBossDefeated"/> and saves.
        /// </summary>
        public void NotifyBossDefeated()
        {
            OnBossDefeated?.Invoke(_currentLevel);
            SaveSystem.SaveGame(BuildSaveData());
        }

        /// <summary>
        /// Move into the <see cref="GameState.Dead"/> state.
        /// Idempotent – safe to call multiple times.
        /// </summary>
        public void TriggerPlayerDeath()
        {
            if (_isGameOver) return;
            _isGameOver = true;
            _stateMachine.ChangeState(GameState.Dead);
        }

        /// <summary>
        /// Restart the current level (e.g. from the death-screen "Retry" button).
        /// Clears the game-over flag and reloads the same level index.
        /// </summary>
        public void RestartFromDeath()
        {
            _isGameOver = false;
            LoadLevel(_currentLevel);
        }

        /// <summary>
        /// Pause gameplay; transitions to <see cref="GameState.Pause"/>.
        /// Only works when currently in Play or Shrine.
        /// </summary>
        public void PauseGame()
        {
            if (CurrentGameState == GameState.Play || CurrentGameState == GameState.Shrine)
                _stateMachine.ChangeState(GameState.Pause);
        }

        /// <summary>
        /// Resume from pause; transitions back to <see cref="GameState.Play"/>.
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentGameState == GameState.Pause)
                _stateMachine.ChangeState(GameState.Play);
        }

        /// <summary>
        /// Open the Options screen from wherever we currently are.
        /// </summary>
        public void OpenOptions()
        {
            _stateMachine.ChangeState(GameState.Options);
        }

        /// <summary>
        /// Close Options; return to Pause if in-game, otherwise Title.
        /// </summary>
        public void CloseOptions()
        {
            GameState returnTo = (_currentLevel >= 0 && CurrentGameState == GameState.Options)
                ? GameState.Pause
                : GameState.Title;
            _stateMachine.ChangeState(returnTo);
        }

        /// <summary>
        /// Player touches a shrine; saves the game and enters <see cref="GameState.Shrine"/>.
        /// </summary>
        public void EnterShrine()
        {
            if (CurrentGameState != GameState.Play) return;
            SaveSystem.SaveGame(BuildSaveData());
            _stateMachine.ChangeState(GameState.Shrine);
        }

        /// <summary>
        /// Leave the shrine interaction screen and return to gameplay.
        /// </summary>
        public void LeaveShrine()
        {
            if (CurrentGameState == GameState.Shrine)
                _stateMachine.ChangeState(GameState.Play);
        }

        /// <summary>
        /// Return to the title/main-menu screen from any state.
        /// </summary>
        public void ReturnToTitle()
        {
            _stateMachine.ChangeState(GameState.Title);
            SceneManager.LoadScene(titleSceneName);
        }

        /// <summary>
        /// Called by <see cref="LevelTransitionState"/> once its animation completes.
        /// </summary>
        public void FinishLevelTransition()
        {
            _stateMachine.ChangeState(GameState.Play);
        }

        // =====================================================================
        //  Level / zone helpers
        // =====================================================================

        /// <summary>
        /// Returns true if <paramref name="level"/> is a boss level.
        /// Boss levels: 4, 9, 14, 19, 24, 29, 34, 39, 44, 49 (i.e. level % 5 == 4).
        /// </summary>
        public bool IsBossLevel(int level)
            => (level % GameConstants.LevelsPerZone == GameConstants.BossLevelOffset);

        /// <summary>
        /// Returns the <see cref="BossType"/> for the given level.
        /// Only meaningful when <see cref="IsBossLevel"/> is true.
        /// </summary>
        public BossType GetBossType(int level)
        {
            int zoneIndex = Mathf.Clamp(level / GameConstants.LevelsPerZone, 0, GameConstants.TotalZones - 1);
            return (BossType)zoneIndex;
        }

        // =====================================================================
        //  Private helpers
        // =====================================================================

        /// <summary>Compute zone index: floor(level / 5), clamped to [0, 9].</summary>
        private static ZoneType CalculateZone(int level)
        {
            int idx = Mathf.Clamp(level / GameConstants.LevelsPerZone, 0, GameConstants.TotalZones - 1);
            return (ZoneType)idx;
        }

        /// <summary>Compute difficulty: 1 + level × 0.12.</summary>
        private static float CalculateDifficulty(int level)
            => GameConstants.BaseDifficulty + level * GameConstants.DifficultyPerLevel;

        private SaveData BuildSaveData() => new SaveData
        {
            level       = _currentLevel,
            totalShards = _totalShards,
            difficulty  = _difficulty
        };

        // =====================================================================
        //  Editor helpers
        // =====================================================================

#if UNITY_EDITOR
        [ContextMenu("Debug – Jump to Level 0 (Zone 0)")]
        private void Debug_JumpToLevel0() => LoadLevel(0);

        [ContextMenu("Debug – Jump to Level 4 (Boss 0: AshenWarden)")]
        private void Debug_JumpToFirstBoss() => LoadLevel(4);

        [ContextMenu("Debug – Jump to Level 49 (Final Boss: DarkLordDrakar)")]
        private void Debug_JumpToFinalBoss() => LoadLevel(49);

        [ContextMenu("Debug – Print State")]
        private void Debug_PrintState()
            => Debug.Log($"[GameManager] State={CurrentGameState}  Level={_currentLevel}  Zone={_currentZone}  Difficulty={_difficulty:F2}  Shards={_totalShards}");

        [ContextMenu("Debug – Trigger Player Death")]
        private void Debug_TriggerDeath() => TriggerPlayerDeath();

        [ContextMenu("Debug – Print All Boss Levels")]
        private void Debug_PrintBossLevels()
        {
            for (int i = 0; i < GameConstants.TotalLevels; i++)
            {
                if (IsBossLevel(i))
                    Debug.Log($"  Boss level {i} → zone {i / GameConstants.LevelsPerZone} → {GetBossType(i)}");
            }
        }
#endif
    }
}
