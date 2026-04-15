using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// StoneGolem: 80 HP, 25 damage, speed 1, 22x30px.
    /// Slow but massive HP. Ground pound every 4s creates a shockwave.
    /// Screen shake on every other footstep.
    /// Squash/stretch animation via transform.localScale.
    /// </summary>
    public class StoneGolem : EnemyBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────────
        [Header("StoneGolem - Ground Pound")]
        [SerializeField] private float groundPoundInterval  = 4f;
        [SerializeField] private float shockwaveHalfWidth   = 5f;
        [SerializeField] private float shockwaveHeight      = 1.2f;
        [SerializeField] private float shockwaveKnockbackY  = 8f;     // must-jump-over
        [SerializeField] private LayerMask shockwaveLayerMask;        // player layer
        [SerializeField] private GameObject shockwaveVFXPrefab;

        [Header("StoneGolem - Footstep Screen Shake")]
        [SerializeField] private float shakeIntensity = 0.25f;
        [SerializeField] private float shakeDuration  = 0.12f;

        [Header("StoneGolem - Stomp Animation")]
        [SerializeField] private float squashScaleY    = 0.75f;
        [SerializeField] private float stretchScaleY   = 1.25f;
        [SerializeField] private float squashDuration  = 0.08f;
        [SerializeField] private float stretchDuration = 0.1f;

        [Header("StoneGolem - Attack")]
        [SerializeField] private float attackRange   = 1.8f;
        [SerializeField] private float attackCooldown = 2.5f;
        [SerializeField] private Vector2 attackBoxSize   = new Vector2(1.8f, 1.2f);
        [SerializeField] private float   attackBoxOffset = 1.1f;
        [SerializeField] private Vector2 attackKnockback = new Vector2(6f, 3f);
        [SerializeField] private LayerMask playerLayerMask;

        [Header("StoneGolem - Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayerMask;

        // ── State Machine ──────────────────────────────────────────────────────
        private enum State { Walk, Attack, GroundPound }
        private State currentState = State.Walk;

        // ── Runtime ────────────────────────────────────────────────────────────
        private float groundPoundTimer;
        private float attackTimer;
        private bool  isActing;                  // mid-animation flag
        private int   footstepParity;            // 0 or 1 – alternates per step
        private float footstepDistAccum;         // accumulates horizontal movement
        private const float FootstepInterval = 1.0f;  // units between steps
        private Vector3 baseLocalScale;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            maxHP      = 80f;
            damage     = 25f;
            moveSpeed  = 1f;
            knockbackResistance = 0.8f;   // Very heavy
        }

        protected override void Start()
        {
            base.Start();
            groundPoundTimer = groundPoundInterval;
            attackTimer      = attackCooldown * 0.5f;
            baseLocalScale   = transform.localScale;
        }

        // ── EnemyBase Abstract Implementations ────────────────────────────────
        protected override void UpdateAI()
        {
            if (isActing) return;

            // Ground pound takes priority
            groundPoundTimer -= Time.deltaTime;
            if (groundPoundTimer <= 0f && IsGrounded())
            {
                StartCoroutine(PerformGroundPound());
                return;
            }

            switch (currentState)
            {
                case State.Walk:   DoWalk();   break;
                case State.Attack: DoAttack(); break;
            }

            UpdateStateTransitions();
        }

        protected override void OnHit()
        {
            if (animator != null) animator.SetTrigger("Hit");
            // Slight flinch
            StartCoroutine(SquashStretch(squashScaleY, squashDuration));
        }

        protected override void HandleOnDeath()
        {
            if (animator != null) animator.SetTrigger("Die");
            // Death slam
            if (IsGrounded()) StartCoroutine(DeathShake());
        }

        // ── State Methods ──────────────────────────────────────────────────────
        void DoWalk()
        {
            if (playerTransform == null) return;

            float dirX = playerTransform.position.x - transform.position.x;
            float speed = Mathf.Sign(dirX) * moveSpeed;

            FaceDirection(dirX);
            rb.velocity = new Vector2(speed, rb.velocity.y);

            // Accumulate movement for footstep detection
            footstepDistAccum += Mathf.Abs(speed * Time.deltaTime);
            if (footstepDistAccum >= FootstepInterval)
            {
                footstepDistAccum = 0f;
                OnFootstep();
            }
        }

        void DoAttack()
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
                StartCoroutine(PerformMeleeAttack());
        }

        void UpdateStateTransitions()
        {
            if (DistanceToPlayer <= attackRange)
                currentState = State.Attack;
            else
                currentState = State.Walk;
        }

        // ── Footstep ──────────────────────────────────────────────────────────
        void OnFootstep()
        {
            footstepParity++;

            // Screen shake every other footstep
            if (footstepParity % 2 == 0)
            {
                if (ScreenShake.Instance != null)
                    ScreenShake.Instance.Shake(shakeIntensity, shakeDuration);
            }

            // Squash-stretch stomp
            StartCoroutine(SquashStretch(squashScaleY, squashDuration));
        }

        // ── Ground Pound ──────────────────────────────────────────────────────
        IEnumerator PerformGroundPound()
        {
            isActing = true;
            currentState = State.GroundPound;

            rb.velocity = new Vector2(0f, rb.velocity.y);

            if (animator != null) animator.SetTrigger("GroundPound");

            // Stretch up before slam
            yield return StartCoroutine(ScaleTo(
                new Vector3(baseLocalScale.x * 0.85f, baseLocalScale.y * stretchScaleY, baseLocalScale.z),
                stretchDuration));

            // Slam down
            yield return StartCoroutine(ScaleTo(
                new Vector3(baseLocalScale.x * 1.3f, baseLocalScale.y * squashScaleY, baseLocalScale.z),
                squashDuration));

            // Shockwave
            if (IsGrounded())
            {
                SpawnShockwave();

                // Heavy screen shake on impact
                if (ScreenShake.Instance != null)
                    ScreenShake.Instance.Shake(shakeIntensity * 3f, shakeDuration * 3f);
            }

            yield return new WaitForSeconds(0.4f);

            // Return to normal scale
            yield return StartCoroutine(ScaleTo(baseLocalScale, 0.15f));

            groundPoundTimer = groundPoundInterval;
            isActing = false;
            currentState = State.Walk;
        }

        void SpawnShockwave()
        {
            // VFX
            if (shockwaveVFXPrefab != null)
                Instantiate(shockwaveVFXPrefab, transform.position, Quaternion.identity);

            // BoxCast along ground
            Vector2 boxCenter = (Vector2)transform.position + Vector2.down * 0.3f;
            Vector2 boxSize   = new Vector2(shockwaveHalfWidth * 2f, shockwaveHeight);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, shockwaveLayerMask);
            foreach (Collider2D hit in hits)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    // Shockwave knocks player upward – must jump over
                    Vector2 kb = new Vector2(0f, shockwaveKnockbackY);
                    DamageSystem.Instance.DealDamage(target, damage, kb, DamageType.Normal, gameObject);
                }
            }
        }

        // ── Melee Attack ──────────────────────────────────────────────────────
        IEnumerator PerformMeleeAttack()
        {
            isActing = true;

            if (animator != null) animator.SetTrigger("Attack");

            yield return StartCoroutine(SquashStretch(stretchScaleY, stretchDuration));
            yield return new WaitForSeconds(0.25f);

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

            yield return StartCoroutine(ScaleTo(baseLocalScale, 0.15f));
            attackTimer = attackCooldown;
            isActing = false;
        }

        // ── Scale Animations ──────────────────────────────────────────────────
        IEnumerator SquashStretch(float targetYScale, float duration)
        {
            Vector3 targetScale = new Vector3(
                baseLocalScale.x * (1f / targetYScale),   // compensate X to maintain volume
                baseLocalScale.y * targetYScale,
                baseLocalScale.z);
            yield return StartCoroutine(ScaleTo(targetScale, duration));
            yield return StartCoroutine(ScaleTo(baseLocalScale, duration));
        }

        IEnumerator ScaleTo(Vector3 targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = targetScale;
        }

        // ── Ground Check ──────────────────────────────────────────────────────
        bool IsGrounded()
        {
            if (groundCheckPoint == null) return true;
            return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayerMask);
        }

        IEnumerator DeathShake()
        {
            if (ScreenShake.Instance != null)
                ScreenShake.Instance.Shake(shakeIntensity * 5f, 0.5f);
            yield return null;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireCube(
                transform.position + Vector3.down * 0.3f,
                new Vector3(shockwaveHalfWidth * 2f, shockwaveHeight, 0f));
        }
    }
}
