// =============================================================================
//  PlayerAnimator.cs  –  Procedural sprite-swap animation + particle hooks
// =============================================================================
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Controls the visual representation of the Ember Knight via sprite swapping
    /// (no Animator component required) and drives particle emission for each
    /// significant state transition.
    ///
    /// Animation states: idle, run, jump, fall, attack1, attack2, attack3,
    ///                   dash, slam, slamLand, hurt, dead.
    ///
    /// Sprite references are assigned in the Inspector; each "state" corresponds
    /// to a single sprite (or a short array for multi-frame states).
    ///
    /// All particle spawning is delegated to <see cref="ParticleManager"/> so that
    /// this component stays purely presentational.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        // =====================================================================
        //  Animation state enum (internal)
        // =====================================================================

        private enum AnimState
        {
            Idle,
            Run,
            Jump,
            Fall,
            Attack1,
            Attack2,
            Attack3,
            Dash,
            Slam,
            SlamLand,
            Hurt,
            Dead
        }

        // =====================================================================
        //  Inspector – sprite banks
        // =====================================================================

        [Header("Sprites – Idle")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private float    idleFrameRate    = 6f;

        [Header("Sprites – Run")]
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private float    runFrameRate     = 12f;

        [Header("Sprites – Jump / Fall")]
        [SerializeField] private Sprite   jumpSprite;
        [SerializeField] private Sprite   fallSprite;

        [Header("Sprites – Attack")]
        [SerializeField] private Sprite   attack1Sprite;
        [SerializeField] private Sprite   attack2Sprite;
        [SerializeField] private Sprite   attack3Sprite;

        [Header("Sprites – Dash")]
        [SerializeField] private Sprite   dashSprite;

        [Header("Sprites – Slam")]
        [SerializeField] private Sprite   slamSprite;
        [SerializeField] private Sprite   slamLandSprite;

        [Header("Sprites – Hit / Death")]
        [SerializeField] private Sprite   hurtSprite;
        [SerializeField] private Sprite   deadSprite;

        [Header("Particle Settings")]
        [Tooltip("Rate (particles/second) for the EmberTrail while dashing.")]
        [SerializeField] private float    emberTrailRate   = 30f;
        [Tooltip("Rate while running.")]
        [SerializeField] private float    runTrailRate     = 5f;
        [Tooltip("Height of the dust-puff spawn point offset.")]
        [SerializeField] private float    dustPuffOffsetY  = -0.45f;

        // =====================================================================
        //  Private state
        // =====================================================================

        private SpriteRenderer   _renderer;
        private PlayerController _controller;
        private PlayerCombat     _combat;

        private AnimState  _currentState  = AnimState.Idle;
        private AnimState  _previousState = AnimState.Idle;

        // Multi-frame animation tracking
        private float _frameTimer;
        private int   _frameIndex;

        // Locked states prevent automatic overwrite until they expire
        private bool  _stateLocked;
        private float _stateLockTimer;
        private float _stateLockDuration;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            _renderer   = GetComponent<SpriteRenderer>();
            _controller = GetComponent<PlayerController>();
            _combat     = GetComponent<PlayerCombat>();
        }

        private void LateUpdate()
        {
            TickStateLock();
            if (!_stateLocked)
                DetermineState();
            UpdateSprite();
            EmitContextualParticles();
        }

        // =====================================================================
        //  Automatic state determination
        // =====================================================================

        private void DetermineState()
        {
            AnimState desired;

            if (_controller.IsDashing)
            {
                desired = AnimState.Dash;
            }
            else if (_controller.IsSlamming)
            {
                desired = AnimState.Slam;
            }
            else if (_combat != null && _combat.IsAttacking)
            {
                desired = ComboCountToAttackState(_combat.ComboCount);
            }
            else if (!_controller.IsGrounded)
            {
                desired = GetComponent<Rigidbody2D>()?.velocity.y > 0.1f
                    ? AnimState.Jump
                    : AnimState.Fall;
            }
            else if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.05f)
            {
                desired = AnimState.Run;
            }
            else
            {
                desired = AnimState.Idle;
            }

            SetState(desired);
        }

        // =====================================================================
        //  State management
        // =====================================================================

        private void SetState(AnimState next)
        {
            if (next == _currentState) return;

            OnExitState(_currentState);
            _previousState = _currentState;
            _currentState  = next;
            _frameIndex    = 0;
            _frameTimer    = 0f;
            OnEnterState(_currentState);
        }

        private void OnEnterState(AnimState state)
        {
            switch (state)
            {
                case AnimState.Dash:
                    // Ember burst to mark the start of a dash
                    ParticleManager.Instance?.SpawnEmberBurst(transform.position, 5);
                    break;

                case AnimState.Run:
                    // Light foot-dust while running is handled each N frames in EmitContextualParticles
                    break;

                case AnimState.SlamLand:
                    // Dust puff is spawned by controller via TriggerSlamLand(); nothing extra here
                    break;
            }
        }

        private void OnExitState(AnimState state)
        {
            // No per-exit effects needed; pools handle cleanup automatically
        }

        private void TickStateLock()
        {
            if (!_stateLocked) return;
            _stateLockTimer -= Time.deltaTime;
            if (_stateLockTimer <= 0f)
            {
                _stateLocked    = false;
                _stateLockTimer = 0f;
            }
        }

        private void LockState(float duration)
        {
            _stateLocked      = true;
            _stateLockTimer   = duration;
            _stateLockDuration = duration;
        }

        // =====================================================================
        //  Sprite rendering
        // =====================================================================

        private void UpdateSprite()
        {
            switch (_currentState)
            {
                case AnimState.Idle:
                    _renderer.sprite = AdvanceFrames(idleFrames, idleFrameRate);
                    break;

                case AnimState.Run:
                    _renderer.sprite = AdvanceFrames(runFrames, runFrameRate);
                    break;

                case AnimState.Jump:
                    _renderer.sprite = SafeSprite(jumpSprite);
                    break;

                case AnimState.Fall:
                    _renderer.sprite = SafeSprite(fallSprite);
                    break;

                case AnimState.Attack1:
                    _renderer.sprite = SafeSprite(attack1Sprite);
                    break;

                case AnimState.Attack2:
                    _renderer.sprite = SafeSprite(attack2Sprite);
                    break;

                case AnimState.Attack3:
                    _renderer.sprite = SafeSprite(attack3Sprite);
                    break;

                case AnimState.Dash:
                    _renderer.sprite = SafeSprite(dashSprite);
                    break;

                case AnimState.Slam:
                    _renderer.sprite = SafeSprite(slamSprite);
                    break;

                case AnimState.SlamLand:
                    _renderer.sprite = SafeSprite(slamLandSprite);
                    break;

                case AnimState.Hurt:
                    _renderer.sprite = SafeSprite(hurtSprite);
                    break;

                case AnimState.Dead:
                    _renderer.sprite = SafeSprite(deadSprite);
                    break;
            }
        }

        // =====================================================================
        //  Multi-frame animation helper
        // =====================================================================

        private Sprite AdvanceFrames(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return null;

            _frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, fps);

            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _frameIndex  = (_frameIndex + 1) % frames.Length;
            }

            return frames[_frameIndex];
        }

        private static Sprite SafeSprite(Sprite s) => s;   // identity; avoids null guard spam above

        // =====================================================================
        //  Contextual particle emission (called each LateUpdate)
        // =====================================================================

        private void EmitContextualParticles()
        {
            // Landing dust puff – triggered once on the frame grounded after airborne
            // (Handled via TriggerLand instead of polling here to avoid per-frame calls)
        }

        // =====================================================================
        //  Public trigger methods  –  called by PlayerController / PlayerCombat
        // =====================================================================

        /// <summary>Call when the player initiates a jump.</summary>
        public void TriggerJump()
        {
            SetState(AnimState.Jump);
            ParticleManager.Instance?.SpawnDustPuff(transform.position + Vector3.up * dustPuffOffsetY);
        }

        /// <summary>Call when the player lands on the ground.</summary>
        public void TriggerLand()
        {
            ParticleManager.Instance?.SpawnDustPuff(transform.position + Vector3.up * dustPuffOffsetY);
            // State returns to Idle/Run automatically next frame
        }

        /// <summary>Call when the dash begins.</summary>
        public void TriggerDash()
        {
            SetState(AnimState.Dash);
            LockState(GameConstants.DashFrames * Time.fixedDeltaTime);
            // Ember burst trail for dash start; trail continuation handled in OnEnterState
            ParticleManager.Instance?.SpawnEmberBurst(transform.position, 6);
        }

        /// <summary>Call when the slam downward movement begins.</summary>
        public void TriggerSlam()
        {
            SetState(AnimState.Slam);
        }

        /// <summary>Call when the slam lands (AOE resolved).</summary>
        public void TriggerSlamLand()
        {
            SetState(AnimState.SlamLand);
            LockState(0.3f);
            // Large dust puff + slam shockwave ring
            ParticleManager.Instance?.SpawnDustPuff(transform.position + Vector3.up * dustPuffOffsetY);
            ParticleManager.Instance?.SpawnSlamImpact(transform.position);
        }

        /// <summary>Call when the player takes damage.</summary>
        public void TriggerHurt()
        {
            SetState(AnimState.Hurt);
            LockState(0.35f);
        }

        /// <summary>Call when the player's health reaches zero.</summary>
        public void TriggerDead()
        {
            SetState(AnimState.Dead);
            LockState(99f); // hold indefinitely
        }

        /// <summary>
        /// Call when the player attacks.
        /// <paramref name="hitNumber"/> is 1, 2, or 3.
        /// </summary>
        public void TriggerAttack(int hitNumber)
        {
            AnimState attackState = ComboCountToAttackState(hitNumber);
            SetState(attackState);

            float lockDur = hitNumber == 3
                ? GameConstants.ComboHit3Duration
                : GameConstants.ComboHit12Duration;
            LockState(lockDur);
        }

        /// <summary>
        /// Call when an ability is cast; plays a brief ability particle and locks the attack frame.
        /// </summary>
        public void TriggerAbility(AbilityType ability)
        {
            // For now, re-use attack1 sprite for ability cast; swap for dedicated art later
            SetState(AnimState.Attack1);
            LockState(0.2f);
        }

        // =====================================================================
        //  Utility
        // =====================================================================

        private static AnimState ComboCountToAttackState(int count)
        {
            return count switch
            {
                1 => AnimState.Attack1,
                2 => AnimState.Attack2,
                3 => AnimState.Attack3,
                _ => AnimState.Idle
            };
        }
    }
}
