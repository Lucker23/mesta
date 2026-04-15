using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// EmberWraith: 25 HP, 10 damage, speed 1.5, 14x22px.
    /// Flying enemy with spring-physics Y-tracking and homing projectiles.
    /// Does NOT collide with platforms.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EmberWraith : EnemyBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────
        [Header("EmberWraith - Flying")]
        [SerializeField] private float hoverOffsetY = 0.5f;      // hover above player Y
        [SerializeField] private float hoverOffsetVariance = 0.4f;

        [Header("EmberWraith - Spring Physics")]
        [SerializeField] private float springConstant = 5f;
        [SerializeField] private float springDamping = 0.8f;

        [Header("EmberWraith - Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float shootInterval = 1.5f;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float homingStrength = 1.5f;

        [Header("EmberWraith - Layers")]
        [SerializeField] private LayerMask platformLayerMask;

        // ── Runtime ────────────────────────────────────────────────────────
        private float springVelocityY;
        private float shootTimer;
        private float targetHoverY;
        private float hoverRecalcTimer;
        private const float HoverRecalcInterval = 2f;

        // ── Unity Lifecycle ────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            maxHP = 25f;
            damage = 10f;
            moveSpeed = 1.5f;
        }

        protected override void Start()
        {
            base.Start();

            // Disable gravity so the wraith floats
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            // Ignore platform layer collisions
            DisablePlatformCollisions();

            shootTimer = shootInterval * 0.5f; // First shot sooner
            RecalculateTargetHoverY();
        }

        void DisablePlatformCollisions()
        {
            if (col == null) return;

            // Iterate all layers in the platform mask and ignore collisions
            for (int i = 0; i < 32; i++)
            {
                if ((platformLayerMask.value & (1 << i)) != 0)
                {
                    Physics2D.IgnoreLayerCollision(gameObject.layer, i, true);
                }
            }
        }

        // ── EnemyBase Abstract Implementations ────────────────────────────
        protected override void UpdateAI()
        {
            if (playerTransform == null) return;

            UpdateHoverY();
            MoveTowardPlayerX();
            FacePlayer();
            HandleShootTimer();
        }

        protected override void OnHit()
        {
            if (animator != null) animator.SetTrigger("Hit");

            // Knockback should not affect Y spring; apply a small push
            springVelocityY += 2f;
        }

        protected override void HandleOnDeath()
        {
            if (animator != null) animator.SetTrigger("Die");
        }

        // ── Movement ───────────────────────────────────────────────────────
        void UpdateHoverY()
        {
            // Recalculate target hover Y periodically for variation
            hoverRecalcTimer -= Time.deltaTime;
            if (hoverRecalcTimer <= 0f)
                RecalculateTargetHoverY();

            float currentY = transform.position.y;
            float displacement = targetHoverY - currentY;

            // Spring force: F = k * x - d * v
            float springForce = springConstant * displacement - springDamping * springVelocityY;
            springVelocityY += springForce * Time.deltaTime;

            float newY = currentY + springVelocityY * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        void RecalculateTargetHoverY()
        {
            if (playerTransform == null) return;
            float offset = hoverOffsetY + UnityEngine.Random.Range(-hoverOffsetVariance, hoverOffsetVariance);
            targetHoverY = playerTransform.position.y + offset;
            hoverRecalcTimer = HoverRecalcInterval;
        }

        void MoveTowardPlayerX()
        {
            float dirX = playerTransform.position.x - transform.position.x;
            float speed = Mathf.Sign(dirX) * moveSpeed;

            // Damp the X velocity using rigidbody so spring handles Y directly
            rb.velocity = new Vector2(speed, 0f);
        }

        void FacePlayer()
        {
            float dirX = playerTransform.position.x - transform.position.x;
            FaceDirection(dirX);
        }

        // ── Shooting ──────────────────────────────────────────────────────
        void HandleShootTimer()
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                ShootProjectile();
                shootTimer = shootInterval;
            }
        }

        void ShootProjectile()
        {
            if (projectilePrefab == null || playerTransform == null) return;

            GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            Projectile proj = projObj.GetComponent<Projectile>();

            if (proj != null)
            {
                Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                proj.Initialize(
                    direction: dir,
                    speed: projectileSpeed,
                    damage: damage,
                    owner: gameObject,
                    isHoming: true,
                    homingStrength: homingStrength,
                    homingTarget: playerTransform);
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.9f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
    }
}
