// =============================================================================
//  PlayerAbilities.cs  –  Ember Wave, Fire Shield, Inferno, and Soul Blast
// =============================================================================
using System.Collections;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Manages the player's mana pool and all learnable special abilities:
    ///
    /// <list type="bullet">
    ///   <item><b>Ember Wave</b>  – projectile, 15 MP, 1 s cooldown, 1.2× damage.</item>
    ///   <item><b>Fire Shield</b> – absorbs 3 hits, 25 MP, 3 s cooldown.</item>
    ///   <item><b>Inferno</b>     – 5×3 AOE burst, 40 MP, 5 s cooldown, 2× damage.</item>
    ///   <item><b>Soul Blast</b>  – consumes soul meter, charged projectile vs bosses.</item>
    /// </list>
    ///
    /// Abilities are unlocked by level via <see cref="CheckAbilityUnlocks"/>.
    ///
    /// Attach alongside <see cref="PlayerController"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAbilities : MonoBehaviour
    {
        // =====================================================================
        //  Inspector
        // =====================================================================

        [Header("Mana")]
        [SerializeField] private float maxMana            = GameConstants.DefaultMaxMana;
        [SerializeField] private float manaRegenRate      = GameConstants.ManaRegenRate;

        [Header("Soul Meter")]
        [SerializeField] private float maxSoul            = 100f;
        [SerializeField] [Tooltip("Soul gained per enemy hit.")]
        private float soulPerHit                          = 10f;

        [Header("Ember Wave")]
        [SerializeField] private float emberWaveManaCost  = GameConstants.EmberWaveManaCost;
        [SerializeField] private float emberWaveCooldown  = GameConstants.EmberWaveCooldown;
        [SerializeField] private float emberWaveSpeed     = GameConstants.EmberWaveSpeed;
        [SerializeField] private float emberWaveDamageMult = GameConstants.EmberWaveDamageMulti;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Fire Shield")]
        [SerializeField] private float fireShieldManaCost = GameConstants.FireShieldManaCost;
        [SerializeField] private float fireShieldCooldown = GameConstants.FireShieldCooldown;
        [SerializeField] private int   fireShieldMaxHits  = GameConstants.FireShieldMaxHits;
        [SerializeField] private GameObject shieldGlowPrefab;

        [Header("Inferno")]
        [SerializeField] private float infernoManaCost    = GameConstants.InfernoManaCost;
        [SerializeField] private float infernoCooldown    = GameConstants.InfernoCooldown;
        [SerializeField] private float infernoAoeWidth    = GameConstants.InfernoAoeWidth;
        [SerializeField] private float infernoAoeHeight   = GameConstants.InfernoAoeHeight;
        [SerializeField] private float infernoDamageMult  = GameConstants.InfernoDamageMulti;

        [Header("Soul Blast")]
        [SerializeField] private float soulBlastSpeed     = GameConstants.SoulBlastSpeed;
        [SerializeField] private float soulBlastDamageMult = GameConstants.SoulBlastDamageMulti;
        [SerializeField] private GameObject soulBlastPrefab;
        [SerializeField] private float soulBlastSoulCost  = 100f;  // requires full soul meter

        [Header("Layer Masks")]
        [SerializeField] private LayerMask enemyLayer;

        // =====================================================================
        //  Private state
        // =====================================================================

        private PlayerController _controller;
        private PlayerAnimator   _animator;

        // Mana
        private float _currentMana;

        // Soul meter
        private float _currentSoul;

        // Ability cooldown timers (count down to 0 = ready)
        private float _emberWaveTimer;
        private float _fireShieldTimer;
        private float _infernoTimer;
        private float _soulBlastTimer;

        // Fire Shield
        private int   _shieldHitsRemaining;
        private bool  _shieldActive;
        private GameObject _shieldGlowInstance;

        // Coroutines
        private Coroutine _infernoCoroutine;

        // =====================================================================
        //  Public properties
        // =====================================================================

        public float CurrentMana        => _currentMana;
        public float MaxMana            => maxMana;
        public float ManaFraction       => maxMana > 0f ? _currentMana / maxMana : 0f;

        public float CurrentSoul        => _currentSoul;
        public float MaxSoul            => maxSoul;
        public float SoulFraction       => maxSoul > 0f ? _currentSoul / maxSoul : 0f;

        public bool  ShieldActive       => _shieldActive;
        public int   ShieldHitsRemaining => _shieldHitsRemaining;

        public bool  EmberWaveReady     => _emberWaveTimer <= 0f;
        public bool  FireShieldReady    => _fireShieldTimer <= 0f && !_shieldActive;
        public bool  InfernoReady       => _infernoTimer <= 0f;
        public bool  SoulBlastReady     => _soulBlastTimer <= 0f && _currentSoul >= soulBlastSoulCost;

        // =====================================================================
        //  Unity lifecycle
        // =====================================================================

        private void Awake()
        {
            _controller  = GetComponent<PlayerController>();
            _animator    = GetComponent<PlayerAnimator>();
            _currentMana = maxMana;
            _currentSoul = 0f;
        }

        private void Start()
        {
            // Sync unlock flags with the current save-game level on startup
            if (GameManager.Instance != null)
                CheckAbilityUnlocks(GameManager.Instance.Level);
        }

        private void Update()
        {
            TickCooldowns();
            RegenerateMana();
            HandleAbilityInput();
        }

        private void OnDestroy()
        {
            if (_infernoCoroutine != null) StopCoroutine(_infernoCoroutine);
        }

        // =====================================================================
        //  Cooldown ticking
        // =====================================================================

        private void TickCooldowns()
        {
            if (_emberWaveTimer  > 0f) _emberWaveTimer  -= Time.deltaTime;
            if (_fireShieldTimer > 0f) _fireShieldTimer -= Time.deltaTime;
            if (_infernoTimer    > 0f) _infernoTimer    -= Time.deltaTime;
            if (_soulBlastTimer  > 0f) _soulBlastTimer  -= Time.deltaTime;
        }

        // =====================================================================
        //  Mana regeneration
        // =====================================================================

        private void RegenerateMana()
        {
            if (_currentMana < maxMana)
                _currentMana = Mathf.Min(maxMana, _currentMana + manaRegenRate * Time.deltaTime);
        }

        // =====================================================================
        //  Input
        // =====================================================================

        private void HandleAbilityInput()
        {
            // Q – Ember Wave
            if (Input.GetKeyDown(KeyCode.Q))
                TryCastEmberWave();

            // E – Fire Shield
            if (Input.GetKeyDown(KeyCode.E))
                TryCastFireShield();

            // R – Inferno
            if (Input.GetKeyDown(KeyCode.R))
                TryCastInferno();

            // F – Soul Blast (only useful vs bosses after unlock)
            if (Input.GetKeyDown(KeyCode.F) && _controller.canSoulBlast)
                TryCastSoulBlast();
        }

        // =====================================================================
        //  Ember Wave
        // =====================================================================

        /// <summary>
        /// Fires a horizontal ember projectile in the player's facing direction.
        /// Costs <see cref="GameConstants.EmberWaveManaCost"/> MP; 1 s cooldown.
        /// </summary>
        public void TryCastEmberWave()
        {
            if (!EmberWaveReady)          { Debug.Log("[Abilities] Ember Wave on cooldown.");    return; }
            if (_currentMana < emberWaveManaCost) { Debug.Log("[Abilities] Not enough mana.");  return; }

            ConsumeMana(emberWaveManaCost);
            _emberWaveTimer = emberWaveCooldown;

            SpawnProjectile(projectilePrefab, emberWaveSpeed, emberWaveDamageMult);
            _animator?.TriggerAbility(AbilityType.EmberWave);
            ParticleManager.Instance?.SpawnEmberBurst(transform.position, 8);
        }

        // =====================================================================
        //  Fire Shield
        // =====================================================================

        /// <summary>
        /// Activates the Fire Shield, absorbing up to 3 incoming hits.
        /// Costs 25 MP; 3 s cooldown (starts when the shield breaks or expires).
        /// </summary>
        public void TryCastFireShield()
        {
            if (!FireShieldReady)              { return; }
            if (_currentMana < fireShieldManaCost) { Debug.Log("[Abilities] Not enough mana."); return; }

            ConsumeMana(fireShieldManaCost);

            _shieldActive         = true;
            _shieldHitsRemaining  = fireShieldMaxHits;

            // Spawn glow visual
            if (shieldGlowPrefab != null && _shieldGlowInstance == null)
                _shieldGlowInstance = Instantiate(shieldGlowPrefab, transform);

            _animator?.TriggerAbility(AbilityType.FireShield);
            ParticleManager.Instance?.SpawnLightBurst(transform.position);
        }

        /// <summary>
        /// Called by the health system when an incoming hit might be absorbed by the shield.
        /// Returns true if the hit was absorbed (caller should skip normal damage processing).
        /// </summary>
        public bool TryAbsorbHit()
        {
            if (!_shieldActive) return false;

            _shieldHitsRemaining--;
            ParticleManager.Instance?.SpawnHitEffect(transform.position, new Color(1f, 0.6f, 0f));

            if (_shieldHitsRemaining <= 0)
                BreakShield();

            return true;
        }

        private void BreakShield()
        {
            _shieldActive        = false;
            _shieldHitsRemaining = 0;
            _fireShieldTimer     = fireShieldCooldown;

            if (_shieldGlowInstance != null)
            {
                Destroy(_shieldGlowInstance);
                _shieldGlowInstance = null;
            }

            ParticleManager.Instance?.SpawnEmberBurst(transform.position, 12);
        }

        // =====================================================================
        //  Inferno
        // =====================================================================

        /// <summary>
        /// Detonates an AOE explosion centred on the player (5×3 units), 2× damage.
        /// Costs 40 MP; 5 s cooldown.  Ori-style radial particle burst.
        /// </summary>
        public void TryCastInferno()
        {
            if (!InfernoReady)              { return; }
            if (_currentMana < infernoManaCost) { Debug.Log("[Abilities] Not enough mana."); return; }

            ConsumeMana(infernoManaCost);
            _infernoTimer = infernoCooldown;

            if (_infernoCoroutine != null) StopCoroutine(_infernoCoroutine);
            _infernoCoroutine = StartCoroutine(InfernoRoutine());
        }

        private IEnumerator InfernoRoutine()
        {
            _animator?.TriggerAbility(AbilityType.Inferno);
            ParticleManager.Instance?.SpawnEmberBurst(transform.position, 20);

            // Brief wind-up before the explosion
            yield return new WaitForSeconds(0.2f);

            Vector2 boxCenter = (Vector2)transform.position;
            Vector2 boxSize   = new Vector2(infernoAoeWidth, infernoAoeHeight);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyLayer);

            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;
                IDamageable infTarget = hit.GetComponent<IDamageable>();
                if (infTarget == null) continue;
                Vector2 knockDir = ((Vector2)hit.transform.position - boxCenter).normalized;
                float   infDmg   = _controller.GetBaseDamage() * infernoDamageMult;
                DamageSystem.Instance?.DealDamage(infTarget, infDmg, knockDir * 10f, DamageType.Fire, gameObject);
            }

            ParticleManager.Instance?.SpawnBossPhaseEffect(transform.position);
            CameraController.RequestShake(0.4f, 0.3f);
            GameLoop.RequestHitStop(GameConstants.DefaultHitStopFrames + 2);

            _infernoCoroutine = null;
        }

        // =====================================================================
        //  Soul Blast
        // =====================================================================

        /// <summary>
        /// Fires a charged projectile that deals 2.5× damage.
        /// Requires a full soul meter (100 souls); consumes it entirely.
        /// Intended for boss encounters; unlocked at level 20.
        /// </summary>
        public void TryCastSoulBlast()
        {
            if (!SoulBlastReady) { return; }

            _currentSoul   = 0f;
            _soulBlastTimer = 1.0f; // short cooldown to prevent accidental double-cast

            SpawnProjectile(soulBlastPrefab != null ? soulBlastPrefab : projectilePrefab,
                            soulBlastSpeed, soulBlastDamageMult, isSoulBlast: true, damageType: DamageType.Magic);

            _animator?.TriggerAbility(AbilityType.EmberWave); // reuse wave animation; replace with dedicated later
            ParticleManager.Instance?.SpawnSoulAbsorb(transform.position);
        }

        // =====================================================================
        //  Soul gain
        // =====================================================================

        /// <summary>
        /// Add soul to the meter (called by PlayerCombat on hit confirmation).
        /// </summary>
        public void GainSoul(float amount)
        {
            _currentSoul = Mathf.Min(maxSoul, _currentSoul + amount);
        }

        /// <summary>Shorthand: gain the standard amount per hit.</summary>
        public void GainSoulOnHit() => GainSoul(soulPerHit);

        // =====================================================================
        //  Projectile spawning
        // =====================================================================

        private void SpawnProjectile(
            GameObject prefab,
            float speed,
            float damageMult,
            bool isSoulBlast           = false,
            DamageType damageType      = DamageType.Fire)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[PlayerAbilities] Projectile prefab is not assigned.");
                return;
            }

            float   dir      = _controller.FacingRight ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(dir * 0.8f, 0.2f, 0f);

            GameObject proj  = Instantiate(prefab, spawnPos, Quaternion.identity);
            Projectile projComp = proj.GetComponent<Projectile>();

            if (projComp != null)
            {
                float damage = _controller.GetBaseDamage() * damageMult;
                // Matches existing Projectile.Initialize signature:
                // (Vector2 direction, float speed, float damage, GameObject owner,
                //  bool isHoming, float homingStrength, Transform homingTarget,
                //  DamageType damageType, float lifetime)
                projComp.Initialize(
                    direction:      new Vector2(dir, 0f),
                    speed:          speed,
                    damage:         damage,
                    owner:          gameObject,
                    isHoming:       false,
                    homingStrength: 0f,
                    homingTarget:   null,
                    damageType:     damageType,
                    lifetime:       5f);
            }
            else
            {
                // Fallback: just give it a velocity via Rigidbody2D if Projectile component is missing
                Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = new Vector2(dir * speed, 0f);
            }
        }

        // =====================================================================
        //  Mana helpers
        // =====================================================================

        private void ConsumeMana(float amount)
        {
            _currentMana = Mathf.Max(0f, _currentMana - amount);
        }

        /// <summary>
        /// Restore mana directly (e.g. from a food pickup or shrine upgrade).
        /// </summary>
        public void RestoreMana(float amount)
        {
            _currentMana = Mathf.Min(maxMana, _currentMana + amount);
        }

        /// <summary>Increase the max mana cap (shrine upgrades).</summary>
        public void IncreaseMaxMana(float amount)
        {
            maxMana      = Mathf.Max(maxMana, maxMana + amount);
            _currentMana = Mathf.Min(_currentMana, maxMana);
        }

        // =====================================================================
        //  Ability unlock check
        // =====================================================================

        /// <summary>
        /// Called by <see cref="GameManager"/> or the progression system whenever the
        /// player's level changes.  Enables the corresponding ability flags on
        /// <see cref="PlayerController"/> so the controller knows what is available.
        /// </summary>
        /// <summary>
        /// Unlock a specific ability by type. Called by SkillSystem during save-load
        /// restoration or when CheckUnlocks fires an unlock event.
        /// </summary>
        public void UnlockAbility(AbilityType ability)
        {
            // EmberWave, FireShield, Inferno are cast via TryCast methods and are
            // gated by mana + cooldowns. Flagging them unlocked here so the HUD
            // can show them. We use simple booleans stored on this class.
            switch (ability)
            {
                case AbilityType.EmberWave:
                    emberWaveUnlocked = true;
                    break;
                case AbilityType.FireShield:
                    fireShieldUnlocked = true;
                    break;
                case AbilityType.Inferno:
                    infernoUnlocked = true;
                    break;
            }
        }

        // Unlock flags (serialised for editor visibility)
        [Header("Skill Unlock Flags (managed by SkillSystem)")]
        [SerializeField] private bool emberWaveUnlocked  = false;
        [SerializeField] private bool fireShieldUnlocked = false;
        [SerializeField] private bool infernoUnlocked    = false;

        public bool EmberWaveUnlocked  => emberWaveUnlocked;
        public bool FireShieldUnlocked => fireShieldUnlocked;
        public bool InfernoUnlocked    => infernoUnlocked;

        /// <param name="level">0-based current level index.</param>
        public void CheckAbilityUnlocks(int level)
        {
            if (_controller == null) return;

            if (level >= GameConstants.DashUnlockLevel)
                _controller.canDash = true;

            if (level >= GameConstants.WallJumpUnlockLevel)
                _controller.canWallJump = true;

            if (level >= GameConstants.SlamUnlockLevel)
                _controller.canSlam = true;

            if (level >= GameConstants.SoulBlastUnlockLevel)
                _controller.canSoulBlast = true;

            Debug.Log($"[PlayerAbilities] Ability unlocks at level {level}: " +
                      $"Dash={_controller.canDash}  WallJump={_controller.canWallJump}  " +
                      $"Slam={_controller.canSlam}  SoulBlast={_controller.canSoulBlast}");
        }

        // =====================================================================
        //  Editor gizmos
        // =====================================================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Inferno AOE
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            Gizmos.DrawWireCube(transform.position, new Vector3(infernoAoeWidth, infernoAoeHeight, 0f));
        }
#endif
    }
}
