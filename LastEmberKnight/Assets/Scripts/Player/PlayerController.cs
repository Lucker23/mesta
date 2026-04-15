// =============================================================================
//  PlayerController.cs  –  Rigidbody2D movement for the Ember Knight
// =============================================================================
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Handles all physical movement for the player character:
    /// <list type="bullet">
    ///   <item>Horizontal movement with friction when no input is held.</item>
    ///   <item>Variable-height jump, coyote time, and jump-input buffering.</item>
    ///   <item>Double jump (0.88× force).</item>
    ///   <item>Dash: 10 fixed-update frames at 15 u/s, invincible, 0.8 s cooldown.</item>
    ///   <item>Wall-slide (gravity 0.3×) and wall-jump (kicks away from wall).</item>
    ///   <item>Slam: Down+Attack in air → vy = −20, AOE BoxCast on landing.</item>
    /// </list>
    ///
    /// Requires: <see cref="Rigidbody2D"/> and <see cref="CapsuleCollider2D"/> (or BoxCollider2D)
    /// on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        // =====================================================================
        //  Inspector
        // =====================================================================

        [Header("Movement")]
        [SerializeField] private float moveSpeed           = GameConstants.PlayerMoveSpeed;
        [SerializeField] private float jumpForce           = GameConstants.PlayerJumpForce;
        [SerializeField] private float coyoteTime          = GameConstants.CoyoteTime;
        [SerializeField] private float jumpBufferTime      = GameConstants.JumpInputBuffer;
        [SerializeField] private float doubleJumpMultiplier = GameConstants.DoubleJumpMultiplier;

        [Header("Friction / Drag")]
        [SerializeField] private float groundLinearDrag    = 8f;
        [SerializeField] private float airLinearDrag       = 1f;
        [SerializeField] private float idleDragMultiplier  = 2.5f;  // extra drag when no input

        [Header("Dash")]
        [SerializeField] private float dashSpeed           = GameConstants.DashSpeed;
        [SerializeField] private float dashCooldown        = GameConstants.DashCooldown;
        [SerializeField] [Tooltip("Upgrade reduces this at runtime.")]
        private float dashCooldownReduction = 0f;

        [Header("Wall")]
        [SerializeField] private float wallSlideGravity    = GameConstants.WallSlideGravityScale;
        [SerializeField] private float wallJumpForceX      = 9f;
        [SerializeField] private float wallJumpForceY      = 12f;
        [SerializeField] private float wallCheckDistance   = 0.55f;
        [SerializeField] private LayerMask wallLayer;

        [Header("Slam")]
        [SerializeField] private float slamVelocityY       = GameConstants.SlamVelocityY;
        [SerializeField] private float slamAoeHalfWidth    = GameConstants.SlamAoeHalfWidth;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Vector2   groundCheckSize  = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        [Header("Ability Unlock Flags")]
        [SerializeField] public bool canDash      = false;
        [SerializeField] public bool canWallJump  = false;
        [SerializeField] public bool canSlam      = false;
        [SerializeField] public bool canSoulBlast = false;

        // =====================================================================
        //  Private state
        // =====================================================================

        private Rigidbody2D _rb;
        private Collider2D  _col;
        private PlayerCombat    _combat;
        private PlayerAnimator  _animator;

        // Grounded / coyote
        private bool  _isGrounded;
        private bool  _wasGrounded;
        private float _coyoteTimer;

        // Jumps
        private int   _jumpsRemaining;   // 0 = exhausted, 1 = one jump left, 2 = double jump available

        // Jump buffer
        private float _jumpBufferTimer;
        private bool  _jumpBuffered;

        // Dash
        private bool      _isDashing;
        private bool      _isInvincible;      // true during dash
        private float     _dashCooldownTimer;
        private Coroutine _dashCoroutine;
        private int       _dashDirection;     // +1 right / -1 left

        // Wall
        private bool  _isTouchingWallLeft;
        private bool  _isTouchingWallRight;
        private bool  _isWallSliding;

        // Slam
        private bool  _isSlamming;
        private bool  _slamLanded;

        // Misc
        private bool  _facingRight = true;
        private float _defaultGravityScale;
        private float _horizontal;

        // =====================================================================
        //  Public properties
        // =====================================================================

        /// <summary>True the frame the player is touching the ground.</summary>
        public bool IsGrounded    => _isGrounded;

        /// <summary>True while pressing toward a wall in mid-air.</summary>
        public bool IsWallSliding => _isWallSliding;

        /// <summary>True during the active dash frames (player is also invincible).</summary>
        public bool IsDashing     => _isDashing;

        /// <summary>True during the invincibility window (dash or i-frames).</summary>
        public bool IsInvincible  => _isInvincible;

        /// <summary>True while slamming downward.</summary>
        public bool IsSlamming    => _isSlamming;

        /// <summary>True if the player sprite faces right.</summary>
        public bool FacingRight   => _facingRight;

        /// <summary>Jumps available at this moment (0, 1 or 2).</summary>
        public int JumpsRemaining => _jumpsRemaining;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _col      = GetComponent<Collider2D>();
            _combat   = GetComponent<PlayerCombat>();
            _animator = GetComponent<PlayerAnimator>();

            _defaultGravityScale  = _rb.gravityScale;
            _rb.freezeRotation    = true;
        }

        private void Start()
        {
            _jumpsRemaining = 2;
        }

        private void Update()
        {
            GatherInput();
            HandleJumpBuffer();
            UpdateCoyoteTimer();
        }

        private void FixedUpdate()
        {
            if (_isDashing) return;  // physics are overridden during dash

            CheckGrounded();
            CheckWalls();
            ApplyMovement();
            HandleWallSlide();
            ApplyFriction();
        }

        private void OnDestroy()
        {
            if (_dashCoroutine != null) StopCoroutine(_dashCoroutine);
        }

        // =====================================================================
        //  Input gathering (called each Update)
        // =====================================================================

        private void GatherInput()
        {
            _horizontal = Input.GetAxisRaw("Horizontal");

            // --- Jump ---
            if (Input.GetButtonDown("Jump"))
            {
                _jumpBufferTimer = jumpBufferTime;
                _jumpBuffered    = true;
            }

            // Variable jump height: release button early → cut velocity
            if (Input.GetButtonUp("Jump") && _rb.velocity.y > 0f && !_isDashing)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * 0.5f);
            }

            // --- Dash ---
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetButtonDown("Dash"))
            {
                TryDash();
            }

            // --- Slam (Down + Attack while airborne) ---
            bool downHeld = Input.GetAxisRaw("Vertical") < -0.5f;
            bool attackDown = Input.GetButtonDown("Fire1");

            if (canSlam && !_isGrounded && downHeld && attackDown && !_isSlamming)
            {
                StartSlam();
            }

            // --- Flip sprite to face movement direction ---
            if (_horizontal > 0f && !_facingRight) Flip();
            if (_horizontal < 0f && _facingRight)  Flip();
        }

        // =====================================================================
        //  Jump buffer / coyote
        // =====================================================================

        private void HandleJumpBuffer()
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= Time.deltaTime;
                if (_jumpBufferTimer <= 0f)
                    _jumpBuffered = false;
            }

            // Consume buffered jump if grounded this frame (coyote or real ground)
            bool canJumpNow = (_isGrounded || _coyoteTimer > 0f) && _jumpsRemaining > 0;
            if (_jumpBuffered && canJumpNow)
            {
                ExecuteJump(jumpForce);
                _jumpBuffered    = false;
                _jumpBufferTimer = 0f;
            }
            else if (_jumpBuffered && !_isGrounded && !_isWallSliding && _jumpsRemaining > 0)
            {
                // Double jump
                ExecuteJump(jumpForce * doubleJumpMultiplier);
                _jumpBuffered    = false;
                _jumpBufferTimer = 0f;
            }
        }

        private void UpdateCoyoteTimer()
        {
            if (_wasGrounded && !_isGrounded)
                _coyoteTimer = coyoteTime;
            else if (_isGrounded)
                _coyoteTimer = 0f;
            else
                _coyoteTimer -= Time.deltaTime;

            _wasGrounded = _isGrounded;
        }

        // =====================================================================
        //  Jump execution
        // =====================================================================

        private void ExecuteJump(float force)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, force);
            _jumpsRemaining = Mathf.Max(0, _jumpsRemaining - 1);
            _coyoteTimer    = 0f;
            _animator?.TriggerJump();
        }

        // =====================================================================
        //  Movement
        // =====================================================================

        private void ApplyMovement()
        {
            if (_isWallSliding && _horizontal == 0f) return; // let wall slide handle gravity

            float targetVx = _horizontal * moveSpeed;
            _rb.velocity = new Vector2(targetVx, _rb.velocity.y);
        }

        private void ApplyFriction()
        {
            if (!_isGrounded) return;

            // Increase drag when no horizontal input is held
            if (Mathf.Abs(_horizontal) < 0.01f)
                _rb.drag = groundLinearDrag * idleDragMultiplier;
            else
                _rb.drag = groundLinearDrag;
        }

        // =====================================================================
        //  Ground check
        // =====================================================================

        private void CheckGrounded()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics2D.OverlapBox(
                groundCheckPoint != null ? groundCheckPoint.position : transform.position + Vector3.down * 0.6f,
                groundCheckSize,
                0f,
                groundLayer);

            if (_isGrounded && !wasGrounded)
            {
                // Landing
                _jumpsRemaining = 2;
                _rb.drag        = groundLinearDrag;
                _animator?.TriggerLand();

                if (_isSlamming)
                    ResolveSlamLanding();
            }

            if (_isGrounded)
                _rb.gravityScale = _defaultGravityScale;
        }

        // =====================================================================
        //  Wall slide / jump
        // =====================================================================

        private void CheckWalls()
        {
            if (!canWallJump) return;

            Vector2 origin = (Vector2)transform.position;
            _isTouchingWallLeft  = Physics2D.Raycast(origin, Vector2.left,  wallCheckDistance, wallLayer);
            _isTouchingWallRight = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        }

        private void HandleWallSlide()
        {
            if (!canWallJump) return;

            bool pressingIntoWall = (_isTouchingWallLeft  && _horizontal < 0f) ||
                                    (_isTouchingWallRight && _horizontal > 0f);

            _isWallSliding = pressingIntoWall && !_isGrounded && _rb.velocity.y < 0f;

            if (_isWallSliding)
            {
                _rb.gravityScale = wallSlideGravity;
                _jumpsRemaining  = 1; // reset one jump so the player can wall-jump

                // Wall-jump input: jump while sliding
                if (_jumpBuffered)
                {
                    int wallDir = _isTouchingWallLeft ? 1 : -1; // kick away from wall
                    _rb.velocity    = new Vector2(wallDir * wallJumpForceX, wallJumpForceY);
                    _jumpBuffered    = false;
                    _jumpBufferTimer = 0f;
                    _isWallSliding  = false;
                    _rb.gravityScale = _defaultGravityScale;

                    if (wallDir > 0) { if (!_facingRight) Flip(); }
                    else             { if (_facingRight)  Flip(); }

                    _animator?.TriggerJump();
                }
            }
            else if (!_isGrounded)
            {
                _rb.gravityScale = _defaultGravityScale;
            }
        }

        // =====================================================================
        //  Dash
        // =====================================================================

        private void TryDash()
        {
            if (!canDash) return;
            if (_isDashing) return;

            float effectiveCooldown = Mathf.Max(0.1f, dashCooldown - dashCooldownReduction);
            if (_dashCooldownTimer > 0f) return;

            _dashDirection   = _facingRight ? 1 : -1;
            _dashCoroutine   = StartCoroutine(DashRoutine(effectiveCooldown));
        }

        private IEnumerator DashRoutine(float cooldown)
        {
            _isDashing   = true;
            _isInvincible = true;

            float savedGravity  = _rb.gravityScale;
            _rb.gravityScale    = 0f;
            _rb.drag            = 0f;

            _animator?.TriggerDash();

            // Hold dash velocity for DashFrames fixed-update frames
            for (int i = 0; i < GameConstants.DashFrames; i++)
            {
                _rb.velocity = new Vector2(_dashDirection * dashSpeed, 0f);
                yield return new WaitForFixedUpdate();
            }

            _rb.gravityScale = savedGravity;
            _isDashing       = false;
            _isInvincible    = false;
            _dashCooldownTimer = cooldown;

            // Count down the cooldown
            while (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
                yield return null;
            }
            _dashCooldownTimer = 0f;
            _dashCoroutine     = null;
        }

        /// <summary>
        /// Reduces the dash cooldown (called by the upgrade system).
        /// </summary>
        public void ReduceDashCooldown(float reduction)
        {
            dashCooldownReduction = Mathf.Clamp(dashCooldownReduction + reduction, 0f, dashCooldown - 0.1f);
        }

        // =====================================================================
        //  Slam
        // =====================================================================

        private void StartSlam()
        {
            _isSlamming      = true;
            _rb.velocity     = new Vector2(_rb.velocity.x, slamVelocityY);
            _rb.gravityScale = 0f; // gravity override so velocity is constant downward
            _animator?.TriggerSlam();
        }

        private void ResolveSlamLanding()
        {
            _isSlamming      = false;
            _rb.gravityScale = _defaultGravityScale;

            // AOE BoxCast centred on landing point
            Vector2 castOrigin = (Vector2)transform.position;
            Vector2 boxHalf    = new Vector2(slamAoeHalfWidth, 0.5f);
            Collider2D[] hits  = Physics2D.OverlapBoxAll(castOrigin, boxHalf * 2f, 0f, enemyLayer);

            float slamDamage = GetBaseDamage() * GameConstants.SlamDamageMultiplier;
            foreach (Collider2D hit in hits)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target == null) continue;
                Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                Vector2 knockback = knockDir * 8f;
                DamageSystem.Instance?.DealDamage(target, slamDamage, knockback, DamageType.Normal, gameObject);
            }

            // Screen shake
            CameraController.RequestShake(GameConstants.SlamShakeMagnitude, GameConstants.SlamShakeDuration);

            // Slam impact ring + light burst
            ParticleManager.Instance?.SpawnSlamImpact(transform.position);
            ParticleManager.Instance?.SpawnLightBurst(transform.position);

            _animator?.TriggerSlamLand();
        }

        /// <summary>
        /// Returns the player's current base attack damage.
        /// Centralised here so upgrades only need to modify one value.
        /// </summary>
        public float GetBaseDamage() => 10f * (GameManager.Instance != null ? GameManager.Instance.Difficulty : 1f);

        // =====================================================================
        //  Helpers
        // =====================================================================

        private void Flip()
        {
            _facingRight         = !_facingRight;
            Vector3 scale        = transform.localScale;
            scale.x             *= -1f;
            transform.localScale = scale;
        }

        // =====================================================================
        //  Editor gizmos
        // =====================================================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Ground check box
            if (groundCheckPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
            }

            // Slam AOE
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, new Vector3(slamAoeHalfWidth * 2f, 1f, 0f));

            // Wall check rays
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.left  * wallCheckDistance);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * wallCheckDistance);
        }
#endif
    }
}
