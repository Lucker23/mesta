using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// BoneCrawler: 20 HP, 8 damage, speed 5, 18x12px.
    /// Very fast, low profile. Erratic movement. Jump-attacks when close.
    /// </summary>
    public class BoneCrawler : EnemyBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────
        [Header("BoneCrawler - Movement")]
        [SerializeField] private float directionChangeInterval = 0.5f;
        [SerializeField] private float erraticSpeedVariance = 1.5f;   // ± added to base speed

        [Header("BoneCrawler - Jump Attack")]
        [SerializeField] private float jumpAttackRange = 1.5f;
        [SerializeField] private float jumpForceX = 6f;
        [SerializeField] private float jumpForceY = 7f;
        [SerializeField] private float jumpAttackCooldown = 1.2f;
        [SerializeField] private LayerMask playerLayerMask;

        [Header("BoneCrawler - Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.12f;
        [SerializeField] private LayerMask groundLayerMask;

        [Header("BoneCrawler - Knockback On Hit")]
        [SerializeField] private Vector2 contactKnockback = new Vector2(3f, 1.5f);

        // ── Runtime ────────────────────────────────────────────────────────
        private float directionTimer;
        private float currentMoveDir = 1f;          // -1 or 1
        private float jumpAttackTimer;
        private bool isJumping;
        private float currentSpeed;
        private bool hasDealtJumpDamage;

        // ── Unity Lifecycle ────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            maxHP = 20f;
            damage = 8f;
            moveSpeed = 5f;
            knockbackResistance = 0.1f;  // Slightly resistant due to low mass/fast movement
        }

        protected override void Start()
        {
            base.Start();
            currentSpeed = moveSpeed;
            PickNewDirection();
        }

        // ── EnemyBase Abstract Implementations ────────────────────────────
        protected override void UpdateAI()
        {
            HandleJumpAttack();

            if (!isJumping)
                HandleErraticMovement();

            HandleContactDamage();
        }

        protected override void OnHit()
        {
            // Brief stagger – speed reduction for one frame handled in UpdateAI flow
            if (animator != null) animator.SetTrigger("Hit");
        }

        protected override void HandleOnDeath()
        {
            if (animator != null) animator.SetTrigger("Die");
        }

        // ── Erratic Movement ───────────────────────────────────────────────
        void HandleErraticMovement()
        {
            directionTimer -= Time.deltaTime;

            if (directionTimer <= 0f)
                PickNewDirection();

            // Bias toward player
            if (playerTransform != null)
            {
                float toPlayer = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(toPlayer) > 0.5f)
                    currentMoveDir = Mathf.Sign(toPlayer);
            }

            FaceDirection(currentMoveDir);
            rb.velocity = new Vector2(currentMoveDir * currentSpeed, rb.velocity.y);
        }

        void PickNewDirection()
        {
            // Occasionally reverse direction for erratic feel
            if (UnityEngine.Random.value < 0.3f)
                currentMoveDir = -currentMoveDir;
            else if (playerTransform != null)
                currentMoveDir = Mathf.Sign(playerTransform.position.x - transform.position.x);
            else
                currentMoveDir = UnityEngine.Random.value > 0.5f ? 1f : -1f;

            currentSpeed = moveSpeed + UnityEngine.Random.Range(-erraticSpeedVariance, erraticSpeedVariance);
            currentSpeed = Mathf.Max(currentSpeed, 1f);

            directionTimer = directionChangeInterval + UnityEngine.Random.Range(-0.1f, 0.2f);
        }

        // ── Jump Attack ────────────────────────────────────────────────────
        void HandleJumpAttack()
        {
            jumpAttackTimer -= Time.deltaTime;

            if (playerTransform == null) return;
            if (jumpAttackTimer > 0f) return;
            if (!IsGrounded()) return;

            float dist = DistanceToPlayer;
            if (dist <= jumpAttackRange)
            {
                PerformJumpAttack();
            }
        }

        void PerformJumpAttack()
        {
            isJumping = true;
            hasDealtJumpDamage = false;

            float dirX = playerTransform != null
                ? Mathf.Sign(playerTransform.position.x - transform.position.x)
                : currentMoveDir;

            FaceDirection(dirX);

            if (animator != null) animator.SetTrigger("JumpAttack");

            rb.velocity = new Vector2(dirX * jumpForceX, jumpForceY);
            jumpAttackTimer = jumpAttackCooldown;

            StartCoroutine(LandingCheck());
        }

        IEnumerator LandingCheck()
        {
            // Wait until we start falling
            yield return new WaitForSeconds(0.1f);

            while (!IsGrounded())
                yield return null;

            isJumping = false;

            // Damage on landing area
            if (!hasDealtJumpDamage)
            {
                Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, playerLayerMask);
                if (hit != null)
                {
                    float dirX = Mathf.Sign(transform.position.x - hit.transform.position.x);
                    Vector2 kb = new Vector2(-dirX * contactKnockback.x, contactKnockback.y);
                    DamageSystem.Instance.DealDamage(
                        hit.GetComponent<IDamageable>(),
                        damage,
                        kb,
                        DamageType.Normal,
                        gameObject);
                    hasDealtJumpDamage = true;
                }
            }
        }

        // ── Contact Damage ─────────────────────────────────────────────────
        // Continuous damage while overlapping with player during jump
        void HandleContactDamage()
        {
            if (!isJumping || hasDealtJumpDamage) return;

            Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.4f, playerLayerMask);
            if (hit != null)
            {
                float dirX = Mathf.Sign(hit.transform.position.x - transform.position.x);
                Vector2 kb = new Vector2(dirX * contactKnockback.x, contactKnockback.y);
                DamageSystem.Instance.DealDamage(
                    hit.GetComponent<IDamageable>(),
                    damage,
                    kb,
                    DamageType.Normal,
                    gameObject);
                hasDealtJumpDamage = true;
            }
        }

        // ── Ground Check ──────────────────────────────────────────────────
        bool IsGrounded()
        {
            if (groundCheckPoint == null) return true;
            return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayerMask);
        }

        // ── Gizmos ────────────────────────────────────────────────────────
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, jumpAttackRange);
        }
    }
}
