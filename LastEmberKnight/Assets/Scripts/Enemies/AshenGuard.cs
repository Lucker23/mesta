using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// AshenGuard: 35 HP, 14 damage, speed 2.5, 16x24px.
    /// Patrols between points, chases within 5 units, melee attacks.
    /// </summary>
    public class AshenGuard : EnemyBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────
        [Header("AshenGuard - Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 1.2f;

        [Header("AshenGuard - Chase")]
        [SerializeField] private float chaseRange = 5f;
        [SerializeField] private float attackRange = 1.2f;

        [Header("AshenGuard - Attack")]
        [SerializeField] private float attackCooldown = 1.0f;
        [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 1.0f);
        [SerializeField] private float attackBoxOffset = 0.8f;
        [SerializeField] private LayerMask playerLayerMask;

        [Header("AshenGuard - Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayerMask;

        [Header("AshenGuard - Knockback")]
        [SerializeField] private Vector2 attackKnockback = new Vector2(4f, 2f);

        // ── State Machine ─────────────────────────────────────────────────
        private enum State { Patrol, Chase, Attack, Stunned }
        private State currentState = State.Patrol;

        // ── Patrol Internals ───────────────────────────────────────────────
        private int patrolIndex;
        private bool waitingAtPoint;
        private float patrolWaitTimer;

        // ── Attack Internals ──────────────────────────────────────────────
        private float attackTimer;
        private bool isAttacking;

        // ── Stun ──────────────────────────────────────────────────────────
        private float stunTimer;

        // ── Unity Lifecycle ────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            maxHP = 35f;
            damage = 14f;
            moveSpeed = 2.5f;
        }

        // ── EnemyBase Abstract Implementations ────────────────────────────
        protected override void UpdateAI()
        {
            if (!IsGrounded()) return; // Stay on platform

            switch (currentState)
            {
                case State.Patrol:  DoPatrol();  break;
                case State.Chase:   DoChase();   break;
                case State.Attack:  DoAttack();  break;
                case State.Stunned: DoStunned(); break;
            }

            UpdateStateTransitions();
        }

        protected override void OnHit()
        {
            if (animator != null)
                animator.SetTrigger("Hit");
        }

        protected override void HandleOnDeath()
        {
            if (animator != null)
                animator.SetTrigger("Die");
        }

        // ── State Methods ─────────────────────────────────────────────────
        void DoPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                // No patrol points: idle
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }

            if (waitingAtPoint)
            {
                patrolWaitTimer -= Time.deltaTime;
                rb.velocity = new Vector2(0f, rb.velocity.y);
                if (patrolWaitTimer <= 0f)
                    waitingAtPoint = false;
                return;
            }

            Transform target = patrolPoints[patrolIndex];
            float dirX = target.position.x - transform.position.x;

            // Check edge before moving
            if (EnemyAI.IsAtEdge(transform, groundCheckPoint, Mathf.Sign(dirX), groundLayerMask))
            {
                AdvancePatrolPoint();
                return;
            }

            FaceDirection(dirX);
            rb.velocity = new Vector2(Mathf.Sign(dirX) * moveSpeed, rb.velocity.y);

            if (Mathf.Abs(dirX) < 0.2f)
                AdvancePatrolPoint();
        }

        void AdvancePatrolPoint()
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            waitingAtPoint = true;
            patrolWaitTimer = patrolWaitTime;
        }

        void DoChase()
        {
            if (playerTransform == null) { currentState = State.Patrol; return; }

            float dirX = playerTransform.position.x - transform.position.x;

            // Avoid walking off edge during chase
            if (!EnemyAI.IsAtEdge(transform, groundCheckPoint, Mathf.Sign(dirX), groundLayerMask))
            {
                FaceDirection(dirX);
                rb.velocity = new Vector2(Mathf.Sign(dirX) * moveSpeed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        void DoAttack()
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f && !isAttacking)
                StartCoroutine(PerformMeleeAttack());
        }

        void DoStunned()
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                currentState = State.Patrol;
        }

        // ── State Transitions ─────────────────────────────────────────────
        void UpdateStateTransitions()
        {
            if (currentState == State.Stunned) return;

            float dist = DistanceToPlayer;

            if (dist <= attackRange)
            {
                currentState = State.Attack;
            }
            else if (dist <= chaseRange)
            {
                currentState = State.Chase;
            }
            else
            {
                currentState = State.Patrol;
            }
        }

        // ── Melee Attack ──────────────────────────────────────────────────
        IEnumerator PerformMeleeAttack()
        {
            isAttacking = true;

            if (animator != null)
                animator.SetTrigger("Attack");

            // Small windup
            yield return new WaitForSeconds(0.2f);

            // BoxCast forward
            float facing = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facing * attackBoxOffset;

            Collider2D hit = Physics2D.OverlapBox(origin, attackBoxSize, 0f, playerLayerMask);
            if (hit != null)
            {
                Vector2 kb = new Vector2(facing * attackKnockback.x, attackKnockback.y);
                DamageSystem.Instance.DealDamage(
                    hit.GetComponent<IDamageable>(),
                    damage,
                    kb,
                    DamageType.Normal,
                    gameObject);
            }

            attackTimer = attackCooldown;

            yield return new WaitForSeconds(0.3f);
            isAttacking = false;
        }

        // ── Ground Check ──────────────────────────────────────────────────
        bool IsGrounded()
        {
            if (groundCheckPoint == null) return true;
            return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayerMask);
        }

        public void Stun(float duration)
        {
            currentState = State.Stunned;
            stunTimer = duration;
        }

        // ── Gizmos ────────────────────────────────────────────────────────
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
