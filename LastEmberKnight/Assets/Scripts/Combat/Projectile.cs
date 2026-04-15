using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastEmberKnight
{
    /// <summary>
    /// Generic projectile for both player and enemy use.
    /// Supports straight-line and homing trajectories.
    /// Call Initialize() after instantiation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Light2D        pointLight;
        [SerializeField] private ParticleSystem trailParticles;

        [Header("Default Settings (overridden by Initialize)")]
        [SerializeField] private float defaultSpeed        = 8f;
        [SerializeField] private float defaultDamage       = 5f;
        [SerializeField] private float defaultLifetime     = 4f;
        [SerializeField] private bool  defaultIsHoming     = false;
        [SerializeField] private float defaultHomingStrength = 2f;

        [Header("Hit VFX")]
        [SerializeField] private GameObject hitParticlesPrefab;

        // ── Runtime State ──────────────────────────────────────────────────────
        private float       speed;
        private float       damage;
        private float       lifetime;
        private bool        isHoming;
        private float       homingStrength;
        private Transform   homingTarget;
        private GameObject  owner;
        private DamageType  damageType = DamageType.Normal;

        private Vector2     direction;
        private Rigidbody2D rb;
        private bool        hasHit;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void Start()
        {
            // If Initialize was not called externally, use defaults
            if (!hasHit && speed == 0f)
            {
                speed          = defaultSpeed;
                damage         = defaultDamage;
                lifetime       = defaultLifetime;
                isHoming       = defaultIsHoming;
                homingStrength = defaultHomingStrength;
            }

            Destroy(gameObject, lifetime > 0f ? lifetime : defaultLifetime);
        }

        private void FixedUpdate()
        {
            if (hasHit) return;

            if (isHoming && homingTarget != null)
            {
                Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
                direction = Vector2.Lerp(direction, toTarget, homingStrength * Time.fixedDeltaTime).normalized;
            }

            rb.velocity = direction * speed;

            // Rotate sprite to face movement direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // ── Public Init ───────────────────────────────────────────────────────
        public void Initialize(Vector2 direction, float speed, float damage,
                               GameObject owner,
                               bool isHoming = false, float homingStrength = 2f,
                               Transform homingTarget = null,
                               DamageType damageType = DamageType.Normal,
                               float lifetime = 4f)
        {
            this.direction      = direction.normalized;
            this.speed          = speed;
            this.damage         = damage;
            this.owner          = owner;
            this.isHoming       = isHoming;
            this.homingStrength = homingStrength;
            this.homingTarget   = homingTarget;
            this.damageType     = damageType;
            this.lifetime       = lifetime;

            ApplyVisualColor(damageType);
        }

        // ── Collision ─────────────────────────────────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

            // Don't hit the owner
            if (owner != null && other.gameObject == owner) return;

            // Don't hit other projectiles
            if (other.GetComponent<Projectile>() != null) return;

            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                // Don't damage entities on the same team
                if (!ShouldDamage(other.gameObject)) return;

                Vector2 kb = direction * 3f;
                if (DamageSystem.Instance != null)
                    DamageSystem.Instance.DealDamage(target, damage, kb, damageType, owner);
            }

            SpawnHitParticles();
            hasHit = true;
            DestroyProjectile();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private bool ShouldDamage(GameObject target)
        {
            if (owner == null) return true;

            bool ownerIsPlayer = owner.CompareTag("Player");
            bool targetIsPlayer = target.CompareTag("Player");
            bool targetIsEnemy  = target.CompareTag("Enemy");

            // Player projectiles damage enemies; enemy projectiles damage the player
            if (ownerIsPlayer && targetIsEnemy)  return true;
            if (!ownerIsPlayer && targetIsPlayer) return true;

            return false;
        }

        private void SpawnHitParticles()
        {
            if (hitParticlesPrefab != null)
                Instantiate(hitParticlesPrefab, transform.position, Quaternion.identity);
        }

        private void DestroyProjectile()
        {
            if (trailParticles != null)
            {
                trailParticles.transform.SetParent(null);
                trailParticles.Stop();
                Destroy(trailParticles.gameObject, 2f);
            }

            Destroy(gameObject);
        }

        private void ApplyVisualColor(DamageType type)
        {
            Color col = type switch
            {
                DamageType.Fire      => new Color(1f, 0.4f, 0.1f),
                DamageType.Ice       => new Color(0.4f, 0.85f, 1f),
                DamageType.Magic     => new Color(0.7f, 0.3f, 1f),
                DamageType.Slash     => Color.white,
                DamageType.Explosion => new Color(1f, 0.6f, 0.1f),
                _                    => Color.white,
            };

            if (spriteRenderer != null) spriteRenderer.color = col;

            if (pointLight != null)
            {
                pointLight.color = col;
                pointLight.intensity = 1.5f;
            }

            if (trailParticles != null)
            {
                var main = trailParticles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(col);
            }
        }
    }
}
