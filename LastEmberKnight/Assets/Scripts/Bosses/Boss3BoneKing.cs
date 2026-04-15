using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Boss 3 – Bone King (Zone 3, Level 19).
    /// An undead warlord who reincarnates once when slain.
    /// 350 HP / 22 damage / 32x40 size.
    /// Phase 2: REINCARNATION – revives at 40% HP with a slow aura.
    /// Player must kill him twice.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Boss3BoneKing : BossBase
    {
        // ── Inspector Fields ──────────────────────────────────────────────

        [Header("Bone King – Stats")]
        [SerializeField] private float moveSpeed = 2.8f;

        [Header("Wraithfire Blast")]
        [SerializeField] private GameObject wraithfireProjectilePrefab;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float projectileCooldown = 4f;
        [SerializeField] private float stunDuration = 0.5f;

        [Header("Vampiric Strike")]
        [SerializeField] private int vampHealHitInterval = 4;
        [SerializeField] private float vampHealPercent = 0.20f;
        [SerializeField] private float meleeRange = 1.6f;
        [SerializeField] private float meleeCooldown = 1.4f;

        [Header("Reincarnation")]
        [SerializeField] private float reincarnationHPPercent = 0.40f;
        [SerializeField] private GameObject reincarnationParticlesPrefab;
        [SerializeField] private float slowAuraRadius = 4f;
        [SerializeField] private float slowAuraAmount = 0.40f;

        [Header("Layers")]
        [SerializeField] private LayerMask playerLayer;

        [Header("Colors")]
        [SerializeField] private Color phase1Color = new Color(0.55f, 0.05f, 0.60f, 1f);
        [SerializeField] private Color phase2Color = new Color(0.30f, 0.02f, 0.35f, 1f);

        // ── Runtime State ──────────────────────────────────────────────────

        private BossAI _ai;
        private float _projectileTimer;
        private float _meleeTimer;
        private int _hitCount;
        private bool _hasBonusLife = true;  // Reincarnation flag
        private bool _reincarnating;
        private bool _isActing;
        private bool _slowAuraApplied;

        // ── BossBase Abstract Implementations ─────────────────────────────

        public override string GetDisplayName() => "Bone King";

        protected override void OnPhaseTransition()
        {
            // Phase 2 is the post-reincarnation state
            if (spriteRenderer != null)
                spriteRenderer.color = phase2Color;

            Debug.Log("[BoneKing] Phase 2 – Reincarnation power active, slow aura emanating.");
        }

        protected override void ExecuteAttack() { }

        // ── Override Die to Handle Reincarnation ──────────────────────────

        protected override void Die()
        {
            if (_hasBonusLife && !_reincarnating)
            {
                StartCoroutine(Reincarnate());
                return;
            }

            // Second death – truly dead
            base.Die();
        }

        private IEnumerator Reincarnate()
        {
            _reincarnating = true;
            _hasBonusLife  = false;
            _isActing      = true;
            isInvincible   = true;

            if (animator != null) animator.SetTrigger("Reincarnate");

            if (reincarnationParticlesPrefab != null)
            {
                GameObject fx = Instantiate(reincarnationParticlesPrefab,
                    transform.position, Quaternion.identity);
                Destroy(fx, 3f);
            }

            yield return new WaitForSeconds(2f);

            // Revive
            float reincHP = maxHP * reincarnationHPPercent;
            currentHP      = reincHP;
            isDead         = false;
            isInvincible   = false;
            _reincarnating = false;
            _isActing      = false;

            Debug.Log("[BoneKing] Reincarnated with 40% HP! Slow aura now active.");

            // Trigger phase 2 if not already
            if (phase == 1)
            {
                phase = 2;
                OnPhaseTransition();
            }
        }

        // ── Unity Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            bossName    = "Bone King";
            bossIndex   = 3;
            maxHP       = 350f;
            damage      = 22f;
            isElderFlag = false;

            _ai = GetComponent<BossAI>();
            if (_ai == null) _ai = gameObject.AddComponent<BossAI>();
        }

        protected override void Start()
        {
            base.Start();
            transform.localScale = new Vector3(32f / 32f, 40f / 32f, 1f);
            if (spriteRenderer != null)
                spriteRenderer.color = phase1Color;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || _isActing || _reincarnating) return;

            _projectileTimer -= Time.deltaTime;
            _meleeTimer      -= Time.deltaTime;

            // Phase 2 slow aura
            if (phase == 2) ApplySlowAura();

            float dist = DistanceToPlayer;

            if (_projectileTimer <= 0f && dist > 3f)
            {
                StartCoroutine(FireWraithfireBlast());
                return;
            }

            if (_meleeTimer <= 0f && dist <= meleeRange)
            {
                StartCoroutine(PerformVampiricStrike());
                return;
            }

            if (dist > meleeRange)
                _ai.MoveTowardPlayer(moveSpeed);
        }

        // ── Attack 1: Wraithfire Blast ─────────────────────────────────────

        private IEnumerator FireWraithfireBlast()
        {
            _isActing        = true;
            _projectileTimer = projectileCooldown;

            if (animator != null) animator.SetTrigger("FireBlast");

            yield return new WaitForSeconds(0.3f);

            if (playerTransform != null && wraithfireProjectilePrefab != null)
            {
                Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                GameObject proj = Instantiate(wraithfireProjectilePrefab,
                    transform.position + (Vector3)(dir * 0.8f), Quaternion.identity);

                WraithfireBlast blastScript = proj.GetComponent<WraithfireBlast>();
                if (blastScript == null) blastScript = proj.AddComponent<WraithfireBlast>();
                blastScript.Initialise(dir, projectileSpeed, damage * 1.2f,
                    stunDuration, playerLayer, gameObject);
            }

            yield return new WaitForSeconds(0.4f);
            _isActing = false;
        }

        // ── Attack 2: Vampiric Strike ──────────────────────────────────────

        private IEnumerator PerformVampiricStrike()
        {
            _isActing   = true;
            _meleeTimer = meleeCooldown;
            _hitCount++;

            if (animator != null) animator.SetTrigger("Melee");

            yield return new WaitForSeconds(0.2f);

            float facingX = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
            Vector2 origin = (Vector2)transform.position + Vector2.right * facingX;
            Collider2D hit = Physics2D.OverlapBox(origin, new Vector2(1.6f, 1.4f), 0f, playerLayer);

            if (hit != null)
            {
                IDamageable dmg = hit.GetComponent<IDamageable>();
                if (dmg != null)
                    DamageSystem.Instance?.DealDamage(dmg, damage,
                        new Vector2(facingX * 3f, 1f), DamageType.Normal, gameObject);

                // Every 4th hit heals boss 20% of damage dealt
                if (_hitCount % vampHealHitInterval == 0)
                {
                    float healAmount = damage * vampHealPercent;
                    currentHP = Mathf.Min(currentHP + healAmount, maxHP);
                    Debug.Log($"[BoneKing] Vampiric Strike healed {healAmount:F1} HP.");
                }
            }

            yield return new WaitForSeconds(0.4f);
            _isActing = false;
        }

        // ── Slow Aura (Phase 2) ────────────────────────────────────────────

        private void ApplySlowAura()
        {
            if (playerTransform == null) return;

            bool inRange = DistanceToPlayer <= slowAuraRadius;

            if (inRange && !_slowAuraApplied)
            {
                _slowAuraApplied = true;
                PlayerMovementDebuff debuff = playerTransform.GetComponent<PlayerMovementDebuff>();
                if (debuff == null) debuff = playerTransform.gameObject.AddComponent<PlayerMovementDebuff>();
                debuff.ApplySlowDebuff(slowAuraAmount, "BoneKingAura");
            }
            else if (!inRange && _slowAuraApplied)
            {
                _slowAuraApplied = false;
                PlayerMovementDebuff debuff = playerTransform.GetComponent<PlayerMovementDebuff>();
                debuff?.RemoveSlowDebuff("BoneKingAura");
            }
        }

        protected override void HandleOnBossDefeated()
        {
            // Remove slow aura
            if (_slowAuraApplied && playerTransform != null)
            {
                PlayerMovementDebuff debuff = playerTransform.GetComponent<PlayerMovementDebuff>();
                debuff?.RemoveSlowDebuff("BoneKingAura");
            }
        }

        // ── Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.6f, 0f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, slowAuraRadius);
        }
#endif
    }

    // ── Helper Components ──────────────────────────────────────────────────

    /// <summary>
    /// Slow-moving wraithfire projectile that stuns the player on hit.
    /// </summary>
    public class WraithfireBlast : MonoBehaviour
    {
        private Vector2 _direction;
        private float _speed;
        private float _damage;
        private float _stunDuration;
        private LayerMask _playerLayer;
        private GameObject _owner;
        private bool _hit;

        public void Initialise(Vector2 direction, float speed, float damage, float stunDuration,
            LayerMask playerLayer, GameObject owner)
        {
            _direction   = direction;
            _speed       = speed;
            _damage      = damage;
            _stunDuration = stunDuration;
            _playerLayer = playerLayer;
            _owner       = owner;

            Destroy(gameObject, 6f);
        }

        private void Update()
        {
            if (_hit) return;
            transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hit) return;

            int otherLayer = 1 << other.gameObject.layer;
            if ((otherLayer & _playerLayer.value) == 0) return;

            _hit = true;

            IDamageable dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
                DamageSystem.Instance?.DealDamage(dmg, _damage, Vector2.zero,
                    DamageType.Normal, _owner);

            // Stun the player
            PlayerMovementDebuff stun = other.GetComponent<PlayerMovementDebuff>();
            if (stun == null) stun = other.gameObject.AddComponent<PlayerMovementDebuff>();
            stun.ApplyStun(_stunDuration);

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lightweight component that applies movement debuffs to the player.
    /// Integrates with PlayerController if present, otherwise is a no-op stub.
    /// </summary>
    public class PlayerMovementDebuff : MonoBehaviour
    {
        private System.Collections.Generic.Dictionary<string, float> _slowDebuffs
            = new System.Collections.Generic.Dictionary<string, float>();

        private bool _isStunned;

        public bool IsStunned => _isStunned;

        public void ApplySlowDebuff(float slowFraction, string key)
        {
            _slowDebuffs[key] = slowFraction;
        }

        public void RemoveSlowDebuff(string key)
        {
            _slowDebuffs.Remove(key);
        }

        public void ApplyStun(float duration)
        {
            StartCoroutine(StunCoroutine(duration));
        }

        public float GetSpeedMultiplier()
        {
            float total = 1f;
            foreach (var v in _slowDebuffs.Values)
                total *= (1f - v);
            return Mathf.Clamp01(total);
        }

        private IEnumerator StunCoroutine(float duration)
        {
            _isStunned = true;
            yield return new WaitForSeconds(duration);
            _isStunned = false;
        }
    }
}
