// =============================================================================
//  StateMachine.cs  –  Generic, key-driven state machine with transitions
// =============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    // =========================================================================
    //  IState  –  contract every concrete state must satisfy
    // =========================================================================

    /// <summary>
    /// Interface implemented by every state object registered with
    /// <see cref="StateMachine{TKey}"/>.  Mirrors the Unity MonoBehaviour lifecycle
    /// so individual states feel natural to write.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when the machine transitions INTO this state.</summary>
        void Enter();

        /// <summary>Called every frame while this state is active (from MonoBehaviour.Update).</summary>
        void Update();

        /// <summary>Called every physics frame while this state is active (from MonoBehaviour.FixedUpdate).</summary>
        void FixedUpdate();

        /// <summary>Called once when the machine transitions OUT OF this state.</summary>
        void Exit();
    }

    // =========================================================================
    //  Transition<TKey>  –  a guarded, directed edge in the state graph
    // =========================================================================

    /// <summary>
    /// Represents a conditional link from one state key to another.
    /// The machine evaluates <see cref="Condition"/> every Update; if it returns
    /// <c>true</c> the machine transitions to <see cref="To"/>.
    /// </summary>
    public class Transition<TKey>
    {
        /// <summary>Destination state key.</summary>
        public TKey       To        { get; }

        /// <summary>Guard condition evaluated each frame. Must not throw.</summary>
        public Func<bool> Condition { get; }

        /// <param name="to">State to move to when the condition is met.</param>
        /// <param name="condition">Predicate evaluated each Update tick.</param>
        public Transition(TKey to, Func<bool> condition)
        {
            To        = to;
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }
    }

    // =========================================================================
    //  StateMachine<TKey>  –  core generic state machine
    // =========================================================================

    /// <summary>
    /// Generic, non-MonoBehaviour state machine that owns a set of <see cref="IState"/>
    /// implementations keyed by an arbitrary type (typically an enum).
    ///
    /// Features:
    /// <list type="bullet">
    ///   <item>Per-state and "any-state" guarded transitions, evaluated each Update.</item>
    ///   <item>Manual <see cref="ChangeState"/> override that bypasses conditions.</item>
    ///   <item><see cref="OnStateChanged"/> event fired on every transition.</item>
    ///   <item>Safe to call ChangeState during a state's Enter() or Exit().</item>
    /// </list>
    ///
    /// Typical usage inside a MonoBehaviour:
    /// <code>
    ///   var sm = new StateMachine&lt;GameState&gt;();
    ///   sm.AddState(GameState.Title, new TitleState(this));
    ///   sm.AddTransition(GameState.Title, GameState.Play, () => _startPressed);
    ///   sm.SetInitialState(GameState.Title);
    ///   // each frame:
    ///   sm.Update();
    ///   // physics:
    ///   sm.FixedUpdate();
    ///   // manual jump:
    ///   sm.ChangeState(GameState.Play);
    /// </code>
    /// </summary>
    public class StateMachine<TKey>
    {
        // =====================================================================
        //  Events
        // =====================================================================

        /// <summary>Fired after every successful transition, passing (previousKey, newKey).</summary>
        public event Action<TKey, TKey> OnStateChanged;

        // =====================================================================
        //  Private storage
        // =====================================================================

        private readonly Dictionary<TKey, IState>                 _states         = new Dictionary<TKey, IState>();
        private readonly Dictionary<TKey, List<Transition<TKey>>> _stateTransitions = new Dictionary<TKey, List<Transition<TKey>>>();
        private readonly List<Transition<TKey>>                   _anyTransitions  = new List<Transition<TKey>>();

        private IState _currentState;
        private TKey   _currentKey;
        private bool   _isRunning;

        // Guards against re-entrancy if ChangeState is called from within Enter/Exit
        private bool _changePending;
        private TKey _pendingKey;

        // =====================================================================
        //  Public read-only accessors
        // =====================================================================

        /// <summary>The key (enum value) of the currently active state.</summary>
        public TKey   CurrentKey   => _currentKey;

        /// <summary>The currently active <see cref="IState"/> object.</summary>
        public IState CurrentState => _currentState;

        /// <summary>False before <see cref="SetInitialState"/> or after <see cref="Stop"/>.</summary>
        public bool   IsRunning    => _isRunning;

        // =====================================================================
        //  Configuration API  –  call before SetInitialState
        // =====================================================================

        /// <summary>
        /// Register a state with the given key.  Replaces any previously registered
        /// state with the same key (logs a warning).
        /// </summary>
        public void AddState(TKey key, IState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (_states.ContainsKey(key))
                Debug.LogWarning($"[StateMachine<{typeof(TKey).Name}>] State '{key}' already registered – overwriting.");

            _states[key] = state;
        }

        /// <summary>
        /// Add a guarded transition that is only evaluated while <paramref name="from"/> is active.
        /// </summary>
        public void AddTransition(TKey from, TKey to, Func<bool> condition)
        {
            if (!_stateTransitions.ContainsKey(from))
                _stateTransitions[from] = new List<Transition<TKey>>();

            _stateTransitions[from].Add(new Transition<TKey>(to, condition));
        }

        /// <summary>
        /// Add a transition that is evaluated regardless of the current state
        /// (higher priority than per-state transitions).
        /// </summary>
        public void AddAnyTransition(TKey to, Func<bool> condition)
        {
            _anyTransitions.Add(new Transition<TKey>(to, condition));
        }

        // =====================================================================
        //  Startup
        // =====================================================================

        /// <summary>
        /// Set the starting state and call its <see cref="IState.Enter"/>.
        /// Must be called exactly once before the first <see cref="Update"/> tick.
        /// </summary>
        public void SetInitialState(TKey key)
        {
            if (!_states.TryGetValue(key, out IState state))
            {
                Debug.LogError($"[StateMachine<{typeof(TKey).Name}>] SetInitialState: key '{key}' not registered.");
                return;
            }

            _currentKey   = key;
            _currentState = state;
            _isRunning    = true;
            _currentState.Enter();
        }

        // =====================================================================
        //  Tick – call from MonoBehaviour.Update / FixedUpdate
        // =====================================================================

        /// <summary>
        /// Evaluates automatic transitions, then ticks the active state's Update.
        /// Call once per MonoBehaviour.Update.
        /// </summary>
        public void Update()
        {
            if (!_isRunning) return;

            // "Any" transitions have highest priority
            Transition<TKey> triggered = FindTriggeredTransition(_anyTransitions);

            // Per-state transitions evaluated next
            if (triggered == null && _stateTransitions.TryGetValue(_currentKey, out var localList))
                triggered = FindTriggeredTransition(localList);

            if (triggered != null)
            {
                ChangeState(triggered.To);
                return; // skip Update on the old state after a transition
            }

            _currentState?.Update();
        }

        /// <summary>
        /// Ticks the active state's FixedUpdate.
        /// Call once per MonoBehaviour.FixedUpdate.
        /// </summary>
        public void FixedUpdate()
        {
            if (!_isRunning) return;
            _currentState?.FixedUpdate();
        }

        // =====================================================================
        //  Manual state change  –  bypasses condition checks
        // =====================================================================

        /// <summary>
        /// Immediately switch to the state with the given key.
        /// Calls <see cref="IState.Exit"/> on the current state and
        /// <see cref="IState.Enter"/> on the new one.
        /// Silently ignored if the machine is already in that state.
        /// </summary>
        public void ChangeState(TKey key)
        {
            if (!_isRunning)
            {
                Debug.LogWarning($"[StateMachine<{typeof(TKey).Name}>] ChangeState('{key}') called before SetInitialState.");
                return;
            }

            if (!_states.TryGetValue(key, out IState nextState))
            {
                Debug.LogError($"[StateMachine<{typeof(TKey).Name}>] ChangeState: key '{key}' not registered.");
                return;
            }

            // Already in this state – no-op
            if (EqualityComparer<TKey>.Default.Equals(_currentKey, key)) return;

            TKey previousKey = _currentKey;

            _currentState?.Exit();

            _currentKey   = key;
            _currentState = nextState;
            _currentState.Enter();

            OnStateChanged?.Invoke(previousKey, key);
        }

        /// <summary>
        /// Halt the machine: calls Exit on the current state and clears the running flag.
        /// The machine can be restarted by calling <see cref="SetInitialState"/> again.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;
            _currentState?.Exit();
            _currentState = null;
            _isRunning    = false;
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static Transition<TKey> FindTriggeredTransition(List<Transition<TKey>> list)
        {
            // Iterate manually to avoid LINQ allocation in hot path
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Condition())
                    return list[i];
            }
            return null;
        }
    }

    // =========================================================================
    //  BaseState  –  convenience base class
    // =========================================================================

    /// <summary>
    /// Abstract base for concrete states.  Override only the lifecycle methods
    /// you actually need; the others are no-ops by default.
    /// </summary>
    public abstract class BaseState : IState
    {
        public virtual void Enter()       { }
        public virtual void Update()      { }
        public virtual void FixedUpdate() { }
        public virtual void Exit()        { }
    }

    // =========================================================================
    //  Concrete game states for GameManager
    // =========================================================================

    // -------------------------------------------------------------------------
    //  TitleState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Active while the title screen is showing.
    /// Ensures <c>Time.timeScale</c> is restored to 1 (in case the player quit from pause).
    /// </summary>
    public class TitleState : BaseState
    {
        private readonly GameManager _gm;
        public TitleState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → Title");
            Time.timeScale = 1f;
        }
    }

    // -------------------------------------------------------------------------
    //  LevelTransitionState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays the fade/wipe animation between levels.
    /// After <see cref="_transitionDuration"/> seconds, calls
    /// <see cref="GameManager.FinishLevelTransition"/> to enter Play.
    /// </summary>
    public class LevelTransitionState : BaseState
    {
        private readonly GameManager _gm;

        /// <summary>Duration of the transition animation in seconds.</summary>
        private const float TransitionDuration = 1.5f;

        private float _timer;

        public LevelTransitionState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → LevelTransition");
            _timer = 0f;
            Time.timeScale = 1f;
        }

        public override void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= TransitionDuration)
                _gm.FinishLevelTransition();
        }
    }

    // -------------------------------------------------------------------------
    //  PlayState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Normal gameplay state.  Ensures timescale is 1 on entry (resumes from pause).
    /// </summary>
    public class PlayState : BaseState
    {
        private readonly GameManager _gm;
        public PlayState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → Play");
            Time.timeScale = 1f;
        }
    }

    // -------------------------------------------------------------------------
    //  ShrineState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Player is interacting with a shrine save-point.
    /// Game time continues (shrine vendor animations play) but player input is
    /// redirected to the shrine UI.
    /// </summary>
    public class ShrineState : BaseState
    {
        private readonly GameManager _gm;
        public ShrineState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → Shrine");
            Time.timeScale = 1f;
        }
    }

    // -------------------------------------------------------------------------
    //  DeadState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Entered when the player's health reaches zero.
    /// Freezes time and fires the <see cref="GameManager.OnPlayerDied"/> event so
    /// the death-screen UI can animate in unscaled time.
    /// </summary>
    public class DeadState : BaseState
    {
        private readonly GameManager _gm;
        public DeadState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → Dead");
            Time.timeScale = 0f;
            GameManager.OnPlayerDied?.Invoke();
        }

        public override void Exit()
        {
            // Thaw time when leaving the death screen
            Time.timeScale = 1f;
        }
    }

    // -------------------------------------------------------------------------
    //  PauseState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Pauses gameplay by setting <c>Time.timeScale = 0</c>.
    /// Stores the pre-pause scale and restores it on Exit so pause works inside
    /// slow-motion segments too.
    /// </summary>
    public class PauseState : BaseState
    {
        private readonly GameManager _gm;
        private float _prePauseScale;

        public PauseState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → Pause");
            _prePauseScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public override void Exit()
        {
            Debug.Log("[GameState] Pause → resuming");
            Time.timeScale = _prePauseScale;
        }
    }

    // -------------------------------------------------------------------------
    //  OptionsState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Settings / options screen. Time is frozen while viewing options to prevent
    /// accidental gameplay events.
    /// </summary>
    public class OptionsState : BaseState
    {
        private readonly GameManager _gm;
        private float _prePauseScale;

        public OptionsState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → Options");
            _prePauseScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public override void Exit()
        {
            Time.timeScale = _prePauseScale;
        }
    }

    // -------------------------------------------------------------------------
    //  EndState
    // -------------------------------------------------------------------------

    /// <summary>
    /// Victory / credits state. Entered after completing level 49.
    /// </summary>
    public class EndState : BaseState
    {
        private readonly GameManager _gm;
        public EndState(GameManager gm) { _gm = gm; }

        public override void Enter()
        {
            Debug.Log("[GameState] → End (Victory!)");
            Time.timeScale = 1f;
        }
    }
}
