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
        public const float ParryStartupFrames  = 4;         // frames (~0.067s at 60fps)
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
        public const string SaveFileName = "save.json";
        public const string SaveKeyLevel = "level";
        public const string SaveKeyShards= "shards";
    }
}
