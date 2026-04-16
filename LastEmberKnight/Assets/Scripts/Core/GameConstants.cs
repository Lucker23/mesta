namespace LastEmberKnight
{
    /// <summary>
    /// All magic numbers in one place. Agents may have defined some of these
    /// inside GameTypes.cs — this file provides any extras not already there.
    /// </summary>
    public static partial class GameConstants
    {
        // ── General ──────────────────────────────────────────────────────────
        public const int   TotalLevels       = 50;
        public const float DifficultyBase    = 1.0f;
        public const float DifficultyPerLevel= 0.12f;
        public const int   ZoneCount         = 10;
        public const int   LevelsPerZone     = 5;

        // ── Player movement ───────────────────────────────────────────────────
        public const float MoveSpeed         = 6f;
        public const float JumpForce         = 14f;
        public const float CoyoteTime        = 0.12f;
        public const float JumpBuffer        = 0.10f;
        public const float DoubleJumpMult    = 0.88f;
        public const float EarlyJumpCut      = 0.50f;       // velocity multiplier on release
        public const float WallSlideGravity  = 0.3f;
        public const float DashSpeed         = 15f;
        public const int   DashFrames        = 10;
        public const float DashCooldown      = 0.8f;
        public const float SlamVelocityY     = -20f;
        public const float SlamAOEWidth      = 3f;
        public const float SlamDamageMult    = 1.5f;

        // ── Combat ────────────────────────────────────────────────────────────
        public const float ComboHit12Duration  = 0.23f;
        public const float ComboHit12Cooldown  = 0.37f;
        public const float ComboHit3Duration   = 0.33f;
        public const float ComboHit3Cooldown   = 0.53f;
        public const float ComboHit3DamageMult = 1.8f;
        public const float ComboLinkWindow     = 0.6f;
        public const float HitboxReach         = 2.0f;
        public const int   HitStopFrames       = 3;
        public const int   BossHitStopFrames   = 5;
        public const int   ParryStartupFrames  = 4;          // frames (~0.067s at 60fps)
        public const float ParryStunDuration   = 0.5f;
        public const float ParrySlowDuration   = 0.2f;
        public const float ParrySlowScale      = 0.15f;
        public const float IFrameDuration      = 0.2f;
        public const float CritChance          = 0.05f;
        public const float CritMultiplier      = 2.0f;

        // ── Player stats ──────────────────────────────────────────────────────
        public const float PlayerMaxHP         = 100f;
        public const float PlayerMaxMP         = 50f;
        public const float PlayerManaRegen     = 1.2f;
        public const float PlayerAttack        = 20f;
        public const float SoulMeterMax        = 100f;
        public const int   SoulGainPerEnemyHit = 8;
        public const int   SoulGainPerBossHit  = 5;

        // ── Abilities ─────────────────────────────────────────────────────────
        public const float EmberWaveManaCost   = 15f;
        public const float EmberWaveCooldown   = 1.0f;
        public const float EmberWaveSpeed      = 12f;
        public const float EmberWaveDamageMult = 1.2f;
        public const float FireShieldManaCost  = 25f;
        public const float FireShieldCooldown  = 3.0f;
        public const int   FireShieldHits      = 3;
        public const float InfernoManaCost     = 40f;
        public const float InfernoCooldown     = 5.0f;
        public const float InfernoWidth        = 5.0f;
        public const float InfernoHeight       = 3.0f;
        public const float InfernoDamageMult   = 2.0f;

        // ── Ability unlock levels ─────────────────────────────────────────────
        public const int DashUnlockLevel      = 5;
        public const int WallJumpUnlockLevel  = 10;
        public const int SlamUnlockLevel      = 15;
        public const int SoulBlastUnlockLevel = 20;
        public const int EmberWaveUnlockLevel = 0;
        public const int FireShieldUnlockLevel= 8;
        public const int InfernoUnlockLevel   = 18;

        // ── Shrine upgrades ───────────────────────────────────────────────────
        public const int   ShrineMaxHPCost     = 5;
        public const float ShrineMaxHPBonus    = 20f;
        public const int   ShrineAttackCost    = 5;
        public const float ShrineAttackBonus   = 5f;
        public const int   ShrineDashCDCost    = 8;
        public const float ShrineDashCDReduce  = 0.13f;
        public const int   ShrineSoulRateCost  = 6;
        public const int   ShrineSoulRateBonus = 3;
        public const int   ShrineMaxManaCost   = 7;
        public const float ShrineMaxManaBonus  = 10f;

        // ── Food ──────────────────────────────────────────────────────────────
        public const int MaxFoodInventory = 5;

        // ── Scene names ───────────────────────────────────────────────────────
        public const string MainScene = "Main";

        // ── Tags ─────────────────────────────────────────────────────────────
        public const string PlayerTag  = "Player";
        public const string EnemyTag   = "Enemy";
        public const string BossTag    = "Boss";
        public const string GroundTag  = "Ground";
        public const string ShrineTag  = "Shrine";
        public const string ExitTag    = "ExitDoor";
        public const string ShardTag   = "Shard";
        public const string FoodTag    = "Food";

        // ── Save keys ─────────────────────────────────────────────────────────
        public const string SaveFileName     = "save.json";
        public const string SaveKeyLevel     = "level";
        public const string SaveKeyShards    = "shards";
        public const string SaveKeyOptions   = "save_options";
        public const string SaveKeyAbilities = "save_abilities";

        // ── Aliases / compatibility names ─────────────────────────────────────
        // Scripts generated from the original spec used slightly different names.
        // These aliases keep everything compiling without touching 50+ files.

        // Game structure
        public const int   TotalZones           = 10;        // alias for ZoneCount
        public const int   BossLevelOffset       = 4;
        public const float BaseDifficulty        = 1.0f;     // alias for DifficultyBase

        // Scene names
        public const string SceneTitle           = "Title";
        public const string SceneGame            = "Main";   // alias for MainScene
        public const string SceneCredits         = "Credits";

        // Tags (Tag* prefix used by many scripts)
        public const string TagPlayer            = "Player";
        public const string TagEnemy             = "Enemy";
        public const string TagBoss              = "Boss";
        public const string TagGround            = "Ground";
        public const string TagWall              = "Wall";
        public const string TagShrine            = "Shrine";

        // Layer name strings (used with LayerMask.NameToLayer)
        public const string LayerGround          = "Ground";
        public const string LayerEnemy           = "Enemy";
        public const string LayerPlayer          = "Player";
        public const string LayerProjectile      = "Projectile";
        public const string LayerWall            = "Wall";

        // Movement aliases
        public const float PlayerMoveSpeed       = 6f;       // alias for MoveSpeed
        public const float PlayerJumpForce       = 14f;      // alias for JumpForce
        public const float JumpInputBuffer       = 0.10f;    // alias for JumpBuffer
        public const float DoubleJumpMultiplier  = 0.88f;   // alias for DoubleJumpMult
        public const float WallSlideGravityScale = 0.3f;    // alias for WallSlideGravity
        public const float SlamAoeHalfWidth      = 3f;      // alias for SlamAOEWidth
        public const float SlamDamageMultiplier  = 1.5f;    // alias for SlamDamageMult

        // Combat aliases
        public const int   DefaultHitStopFrames  = 3;       // alias for HitStopFrames
        public const float ComboHitboxRange      = 2.0f;   // alias for HitboxReach
        public const float ComboHit3DamageMulti  = 1.8f;   // alias for ComboHit3DamageMult
        public const int   ParryStartupFramesInt = 4;
        public const int   FireShieldMaxHits      = 3;      // alias for FireShieldHits

        // Ability aliases
        public const float EmberWaveDamageMulti  = 1.2f;   // alias for EmberWaveDamageMult
        public const float InfernoAoeWidth        = 5.0f;  // alias for InfernoWidth
        public const float InfernoAoeHeight       = 3.0f;  // alias for InfernoHeight
        public const float InfernoDamageMulti     = 2.0f;  // alias for InfernoDamageMult
        public const float SoulBlastSpeed         = 18.0f;
        public const float SoulBlastDamageMulti   = 2.5f;

        // Mana aliases
        public const float DefaultMaxMana         = 50.0f;  // alias for PlayerMaxMP
        public const float ManaRegenRate          = 1.2f;   // alias for PlayerManaRegen

        // Camera shake
        public const float SlamShakeMagnitude     = 0.35f;
        public const float SlamShakeDuration      = 0.25f;
    }
}
