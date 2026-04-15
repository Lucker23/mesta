using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// ShieldBearer: 50 HP, 18 damage, speed 2, 18x26px.
    /// Has a frontal shield that reduces front-facing damage to 20%.
    /// Attack cycle: approach → lower shield (0.5s window) → attack → raise shield.
    /// Must be hit from behind or above to deal full damage.
    /// </summary>
    public class ShieldBearer : EnemyBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("ShieldBearer - Shield")]
        [SerializeField] private bool  shieldActiveByDefault = true;
        [SerializeField] private float frontDamageReduction  = 0.20f;  // 20% of incoming damage passes through
        [SerializeField] private float shieldLowerDuration   = 0.5f;   // vulnerability window

        [Header("ShieldBearer - Attack")]
        [SerializeField] private float attackRange     = 1.4f;
        [SerializeField] private float attackCooldown  = 1.8f;
        [SerializeField] private float attackWindup    = 0.35f;
        [SerializeField] private Vector2 attackBoxSize = new Vector2(1.4f, 1.0f);
        [SerializeField] private float   attackBoxOffset = 0.9f;
        [SerializeField] private Vector2 attackKnockback  = new Vector2(5f, 2.5f);
        [SerializeField] private LayerMask playerLayerMask;

        [Header("ShieldBearer - Chase")]
        [SerializeField] private float chaseRange = 6f;

        [Header("ShieldBearer - Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayerMask;

        [Header("ShieldBearer - VFX")]
        [SerializeField] private GameObject shieldDownVFX;
        [SerializeField] private SpriteRenderer shieldSprite;   // visual shield object

        // ── State Machine ─────────────────────────────────────────────────────
        private enum State { Idle, Chase, AttackCycle, Stunned }
        private State currentState = State.Idle;

        // ── Runtime ────────────────────────────────────────────────────────────
        private bool   shieldActive;
        private float  attackTimer;
        private bool   isPerformingAttack;
        private float  stunTimer;

        // ── Properties ────────────────────────────────────────────────────────
        public bool ShieldActive => shieldActive;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            maxHP      = 50f;
            damage     = 18f;
            moveSpeed  = 2f;
            knockbackResistance = 0.4f;    // Heavy enemy – resists knockback
        }

        protected override void Start()
        {
            base.Start();
            shieldActive = shieldActiveByDefault;
            attackTimer  = attackCooldown * 0.5f;   // First attack slightly sooner
            UpdateShieldVisual();
        }

        // ── EnemyBase Abstract Implementations ────────────────────────────────
        protected override void UpdateAI()
        {
            switch (currentState)
            {
                case State.Idle:        DoIdle();        break;
                case State.Chase:       DoChase();       break;
                case State.AttackCycle: /* handled by coroutine */ break;
                case State.Stunned:     DoStunned();     break;
            }

            if (currentState != State.Stunned && currentState != State.AttackCycle)
                UpdateStateTransitions();
        }

        protected override void OnHit()
        {
            if (animator != null) animator.SetTrigger("Hit");
        }

        protected override void HandleOnDeath()
        {
            if (animator != null) animator.SetTrigger("Die");
        }

        // ── Shield Damage Override ─────────────────────────────────────────────
        public override void TakeDamage(float amount, Vector2 knockback)
        {
            if (!IsAlive || isInvincible) return;

            float finalAmount = amount;

            if (shieldActive && IsAttackFromFront(knockback))
            {
                // Front attacks deal only 20% damage when shield is up
                finalAmount = amount * frontDamageReduction;
            }

            base.TakeDamage(finalAmount, knockback);
        }

        // ── State Methods ─────────────────────────────────────────────────────
        void DoIdle()
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        void DoChase()
        {
            if (playerTransform == null) return;

            float dirX = playerTransform.position.x - transform.position.x;

            if (!IsAtEdge(dirX))
            {
                FaceDirection(dirX);
                rb.velocity = new Vector2(Mathf.Sign(dirX) * moveSpeed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }

            // Tick attack cooldown
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f && !isPerformingAttack)
                StartCoroutine(AttackCycle());
        }

        void DoStunned()
        {
            rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0f, 10f * Time.deltaTime), rb.velocity.y);
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                currentState = State.Chase;
        }

        // ── State Transitions ─────────────────────────────────────────────────
        void UpdateStateTransitions()
        {
            float dist = DistanceToPlayer;

            if (dist <= chaseRange)
                currentState = State.Chase;
            else
                currentState = State.Idle;
        }

        // ── Attack Cycle ──────────────────────────────────────────────────────
        IEnumerator AttackCycle()
        {
            if (isPerformingAttack) yield break;
            isPerformingAttack = true;
            currentState = State.AttackCycle;

            // 1. Approach until in melee range
            while (DistanceToPlayer > attackRange)
            {
                if (!IsAlive) { isPerformingAttack = false; yield break; }
                float dirX = playerTransform != null
                    ? playerTransform.position.x - transform.position.x
                    : 0f;
                FaceDirection(dirX);
                rb.velocity = new Vector2(Mathf.Sign(dirX) * moveSpeed, rb.velocity.y);
                yield return null;
            }

            rb.velocity = new Vector2(0f, rb.velocity.y);

            // 2. Lower shield – vulnerability window begins
            SetShield(false);
            if (shieldDownVFX != null) Instantiate(shieldDownVFX, transform.position, Quaternion.identity);
            if (animator != null) animator.SetTrigger("LowerShield");

            yield return new WaitForSeconds(shieldLowerDuration);

            // 3. Perform melee attack
            if (IsAlive)
            {
                if (animator != null) animator.SetTrigger("Attack");
                yield return new WaitForSeconds(attackWindup);

                if (IsAlive) PerformMeleeHit();
            }

            yield return new WaitForSeconds(0.3f);

            // 4. Raise shield
            SetShield(true);
            if (animator != null) animator.SetTrigger("RaiseShield");

            attackTimer        = attackCooldown;
            isPerformingAttack = false;
            currentState       = State.Chase;
        }

        void PerformMeleeHit()
        {
            float facing = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facing * attackBoxOffset;
            Collider2D hit = Physics2D.OverlapBox(origin, attackBoxSize, 0f, playerLayerMask);

            if (hit != null)
            {
                Vector2 kb = new Vector2(facing * attackKnockback.x, attackKnockback.y);
                DamageSystem.Instance.DealDamage(
                    hit.GetComponent<IDamageable>(),
                    damage, kb,
                    DamageType.Normal,
                    gameObject);
            }
        }

        // ── Shield Helpers ────────────────────────────────────────────────────
        void SetShield(bool active)
        {
            shieldActive = active;
            UpdateShieldVisual();
        }

        void UpdateShieldVisual()
        {
            if (shieldSprite != null)
                shieldSprite.enabled = shieldActive;
        }

        /// <summary>
        /// Determines if the incoming hit came from the front (the direction the enemy is facing).
        /// knockback direction points away from the attacker, so the attacker is in the opposite direction.
        /// </summary>
        bool IsAttackFromFront(Vector2 knockback)
        {
            if (knockback == Vector2.zero) return false;

            // The knockback pushes the enemy; the attacker is on the side the knockback came from.
            // If the enemy faces right (flipX=false) and knockback is negative X → attacker is to the right → front.
            float facingSign = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            float attackSideSign = -Mathf.Sign(knockback.x);   // opposite of push direction

            return Mathf.Sign(attackSideSign) == Mathf.Sign(facingSign);
        }

        // ── Ground / Edge Check ───────────────────────────────────────────────
        bool IsAtEdge(float moveDir)
        {
            if (groundCheckPoint == null) return false;
            Vector3 edgeCheckPos = groundCheckPoint.position + Vector3.right * Mathf.Sign(moveDir) * 0.3f;
            return !Physics2D.OverlapCircle(edgeCheckPos, groundCheckRadius, groundLayerMask);
        }

        // ── External Stun ────────────────────────────────────────────────────
        public void Stun(float duration)
        {
            SetShield(false);   // shield drops on stun
            currentState = State.Stunned;
            stunTimer    = duration;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
