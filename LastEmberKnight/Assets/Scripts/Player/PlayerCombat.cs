// =============================================================================
//  PlayerCombat.cs  –  3-hit combo, parry system, and damage dealing
// =============================================================================
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Manages the player's melee attack system:
    /// <list type="bullet">
    ///   <item><b>3-hit combo</b> – hits 1-2 share timings; hit 3 is the finisher (1.8× damage, larger knockback).</item>
    ///   <item><b>BoxCast hitbox</b> – 2 units in front of the player each swing.</item>
    ///   <item><b>Hit response</b> – calls <see cref="GameLoop.RequestHitStop"/>, camera punch, and <see cref="ParticleManager"/>.</item>
    ///   <item><b>Parry</b> – if the player attacks during an enemy's startup window (frames 0-4),
    ///         the enemy is stunned 0.5 s and a white particle burst plays.</item>
    /// </list>
    ///
    /// Attach alongside <see cref="PlayerController"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerCombat : MonoBehaviour
    {
        // =====================================================================
        //  Inspector
        // =====================================================================

        [Header("Hitbox")]
        [SerializeField] private Vector2 hitboxSize        = new Vector2(GameConstants.ComboHitboxRange, 0.8f);
        [SerializeField] private Vector2 hitboxOffset      = new Vector2(1.0f, 0.0f);  // local-space, mirrored for left
        [SerializeField] private LayerMask enemyLayer;

        [Header("Combo Timing")]
        [Tooltip("Active window for hits 1 and 2 (seconds).")]
        [SerializeField] private float hit12Duration       = GameConstants.ComboHit12Duration;
        [Tooltip("Recovery time after hits 1 and 2 before next input registers.")]
        [SerializeField] private float hit12Cooldown       = GameConstants.ComboHit12Cooldown;
        [Tooltip("Active window for hit 3 (finisher).")]
        [SerializeField] private float hit3Duration        = GameConstants.ComboHit3Duration;
        [Tooltip("Recovery time after hit 3 – longest in the combo.")]
        [SerializeField] private float hit3Cooldown        = GameConstants.ComboHit3Cooldown;
        [Tooltip("Time window after the last hit during which the next combo input is accepted.")]
        [SerializeField] private float comboLinkWindow     = 0.45f;

        [Header("Knockback")]
        [SerializeField] private float knockbackForceLight = 4f;
        [SerializeField] private float knockbackForceHeavy = 8f;  // hit 3 and parry-stun knockback

        [Header("Parry")]
        [Tooltip("Enemy startup frames during which a parry is valid (0 inclusive to this value).")]
        [SerializeField] private int    parryStartupFrames = GameConstants.ParryStartupFrames;
        [SerializeField] private float  parryStunDuration  = GameConstants.ParryStunDuration;

        [Header("HitStop")]
        [SerializeField] private int hitStopFrames         = GameConstants.DefaultHitStopFrames;

        // =====================================================================
        //  Private state
        // =====================================================================

        private PlayerController _controller;
        private PlayerAnimator   _animator;

        // Combo tracking
        private int   _comboCount;           // 0 = idle, 1 = after hit 1, 2 = after hit 2, 3 = after finisher
        private bool  _isAttacking;
        private bool  _comboInputQueued;     // next attack input arrived during active window
        private float _comboLinkTimer;       // counts down; if it hits 0 combo resets

        private Coroutine _attackCoroutine;

        // =====================================================================
        //  Public properties
        // =====================================================================

        /// <summary>True while the attack animation / hitbox is active.</summary>
        public bool IsAttacking  => _isAttacking;

        /// <summary>Current position in the combo chain (0 = idle, 1-3 = hit index).</summary>
        public int  ComboCount   => _comboCount;

        /// <summary>True when the player may start a new attack or continue the combo.</summary>
        public bool CanAttack    => !_isAttacking || _comboInputQueued == false;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _animator   = GetComponent<PlayerAnimator>();
        }

        private void Update()
        {
            HandleComboLinkTimer();

            // Attack input
            if (Input.GetButtonDown("Fire1"))
            {
                if (!_isAttacking)
                    StartAttack();
                else if (_comboCount < 3)
                    _comboInputQueued = true;   // buffer next attack in combo
            }
        }

        private void OnDestroy()
        {
            if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        }

        // =====================================================================
        //  Combo link window
        // =====================================================================

        private void HandleComboLinkTimer()
        {
            if (_comboLinkTimer > 0f)
            {
                _comboLinkTimer -= Time.deltaTime;
                if (_comboLinkTimer <= 0f)
                    ResetCombo();
            }
        }

        // =====================================================================
        //  Attack entry-point
        // =====================================================================

        private void StartAttack()
        {
            if (_comboCount >= 3) return; // already at the end of the combo

            _comboCount++;
            _comboLinkTimer = 0f;   // clear any pending reset while attacking
            _attackCoroutine = StartCoroutine(AttackRoutine(_comboCount));
        }

        // =====================================================================
        //  Attack coroutine
        // =====================================================================

        private IEnumerator AttackRoutine(int hitNumber)
        {
            _isAttacking = true;
            _comboInputQueued = false;

            // Choose timing based on hit number
            bool   isFinisher = (hitNumber == 3);
            float  activeDur  = isFinisher ? hit3Duration  : hit12Duration;
            float  cooldownDur = isFinisher ? hit3Cooldown : hit12Cooldown;
            float  damageMult = isFinisher ? GameConstants.ComboHit3DamageMulti : 1f;
            float  kbForce    = isFinisher ? knockbackForceHeavy : knockbackForceLight;

            _animator?.TriggerAttack(hitNumber);

            // --- Active hit window ---
            float elapsed = 0f;
            bool hitConfirmed = false;

            while (elapsed < activeDur)
            {
                elapsed += Time.deltaTime;

                if (!hitConfirmed)
                {
                    bool anyHit = PerformHitScan(damageMult, kbForce, out bool parryTriggered);

                    if (parryTriggered)
                    {
                        // Parry overrides normal hit logic
                        yield return null;
                        break;
                    }

                    if (anyHit)
                    {
                        hitConfirmed = true;
                        GameLoop.RequestHitStop(hitStopFrames);
                        CameraController.RequestPunch(_controller.FacingRight ? Vector2.right : Vector2.left, 0.12f);
                    }
                }

                yield return null;
            }

            // --- Cooldown / recovery ---
            _isAttacking = false;

            // If the player queued the next attack during the active window, chain immediately
            if (_comboInputQueued && _comboCount < 3)
            {
                _comboInputQueued = false;
                StartAttack();
                yield break;
            }

            // Otherwise wait for the recovery window before the combo resets
            _comboLinkTimer = comboLinkWindow;

            float recoveryElapsed = 0f;
            while (recoveryElapsed < cooldownDur)
            {
                recoveryElapsed += Time.deltaTime;

                // New input during recovery: chain if still in link window
                if (_comboInputQueued && _comboCount < 3)
                {
                    _comboInputQueued = false;
                    _comboLinkTimer   = 0f;
                    StartAttack();
                    yield break;
                }

                yield return null;
            }

            // Recovery done: if no chained input arrived, let the link timer expire naturally
            _attackCoroutine = null;
        }

        // =====================================================================
        //  Hitbox scan
        // =====================================================================

        /// <returns>True if at least one enemy was struck.</returns>
        private bool PerformHitScan(float damageMultiplier, float knockbackForce, out bool parryTriggered)
        {
            parryTriggered = false;

            float   dir        = _controller.FacingRight ? 1f : -1f;
            Vector2 origin     = (Vector2)transform.position + new Vector2(hitboxOffset.x * dir, hitboxOffset.y);
            Vector2 castDir    = new Vector2(dir, 0f);

            Collider2D[] hits  = Physics2D.OverlapBoxAll(origin, hitboxSize, 0f, enemyLayer);

            if (hits == null || hits.Length == 0) return false;

            bool anyHit = false;

            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;

                // --- Check for parry ---
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsInStartupFrames(parryStartupFrames))
                {
                    TriggerParry(enemy);
                    parryTriggered = true;
                    return false; // parry replaces the attack
                }

                // --- Normal damage ---
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target == null) continue;

                Vector2 knockDir = new Vector2(dir, 0.3f).normalized;
                float   finalDmg = _controller.GetBaseDamage() * damageMultiplier;
                Vector2 knockback = knockDir * knockbackForce;

                DamageSystem.Instance?.DealDamage(target, finalDmg, knockback, DamageType.Slash, gameObject);

                ParticleManager.Instance?.SpawnHitEffect(hit.transform.position, Color.white);
                anyHit = true;
            }

            return anyHit;
        }

        // =====================================================================
        //  Parry
        // =====================================================================

        private void TriggerParry(EnemyBase enemy)
        {
            enemy.ApplyStun(parryStunDuration);

            // Larger knockback on parry
            Vector2 knockDir  = (_controller.FacingRight ? Vector2.right : Vector2.left) + Vector2.up * 0.4f;
            float   parryDmg  = _controller.GetBaseDamage() * 0.5f;
            Vector2 knockback = knockDir.normalized * knockbackForceHeavy;

            IDamageable target = enemy.GetComponent<IDamageable>();
            DamageSystem.Instance?.DealDamage(target, parryDmg, knockback, DamageType.Normal, gameObject);

            // Visual feedback – white light burst for parry
            ParticleManager.Instance?.SpawnLightBurst(enemy.transform.position);
            GameLoop.RequestSlowMotion(0.12f, 0.15f);
            CameraController.RequestShake(0.15f, 0.15f);

            Debug.Log($"[PlayerCombat] Parry! Enemy '{enemy.name}' stunned for {parryStunDuration}s.");
        }

        // =====================================================================
        //  Combo reset
        // =====================================================================

        private void ResetCombo()
        {
            _comboCount       = 0;
            _isAttacking      = false;
            _comboInputQueued = false;
            _comboLinkTimer   = 0f;

            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
        }

        // =====================================================================
        //  Upgrade hooks
        // =====================================================================

        private float _attackBonus = 0f;

        /// <summary>
        /// Add a flat damage bonus to all attacks. Called by UpgradeSystem.
        /// The bonus is added on top of the base damage from PlayerController.GetBaseDamage().
        /// </summary>
        public void AddAttackBonus(float amount)
        {
            _attackBonus += amount;
        }

        /// <summary>
        /// Returns the total attack bonus accumulated from upgrades.
        /// Used internally when computing final hit damage.
        /// </summary>
        public float AttackBonus => _attackBonus;

        // =====================================================================
        //  External interrupt (e.g. player gets stunned / dies)
        // =====================================================================

        /// <summary>
        /// Immediately cancel any ongoing attack and reset combo state.
        /// Called by the health system or stun handler.
        /// </summary>
        public void CancelAttack()
        {
            ResetCombo();
        }

        // =====================================================================
        //  Editor gizmos
        // =====================================================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float dir    = (_controller != null && _controller.FacingRight) ? 1f : -1f;
            Vector2 pos  = (Vector2)transform.position + new Vector2(hitboxOffset.x * dir, hitboxOffset.y);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(pos, hitboxSize);
        }
#endif
    }
}
