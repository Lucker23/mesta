// =============================================================================
//  GameTypes.cs  –  All shared enums, interfaces, and numeric constants for
//                   Last Ember Knight
// =============================================================================
using UnityEngine;

namespace LastEmberKnight
{
    // -------------------------------------------------------------------------
    //  Combat – damage classification
    // -------------------------------------------------------------------------

    /// <summary>
    /// Classifies what kind of damage is being dealt.
    /// Used by DamageSystem for type-specific colour coding, resistances, and VFX.
    /// </summary>
    public enum DamageType
    {
        Normal,
        Fire,
        Ice,
        Magic,
        Slash,
        Explosion
    }

    // -------------------------------------------------------------------------
    //  Combat – damageable interface
    // -------------------------------------------------------------------------

    /// <summary>
    /// Implemented by any GameObject that can receive damage (enemies, bosses, player, destructibles).
    /// </summary>
    public interface IDamageable
    {
        /// <summary>Current hit-points.</summary>
        float HP { get; }

        /// <summary>
        /// Apply damage and knockback to this object.
        /// Implementations are responsible for invincibility frames, death logic, etc.
        /// </summary>
        void TakeDamage(float amount, Vector2 knockback);
    }


    // -------------------------------------------------------------------------
    //  Game flow state enum
    // -------------------------------------------------------------------------

    /// <summary>
    /// Top-level state machine states that drive what code is active at any moment.
    /// </summary>
    public enum GameState
    {
        /// <summary>Main menu / title screen.</summary>
        Title,
        /// <summary>Fade/transition animation between levels.</summary>
        LevelTransition,
        /// <summary>Normal live gameplay.</summary>
        Play,
        /// <summary>Player is interacting with a shrine (save point, upgrade screen).</summary>
        Shrine,
        /// <summary>Player has died; death screen visible.</summary>
        Dead,
        /// <summary>Game is paused (pause menu overlay).</summary>
        Pause,
        /// <summary>Options / settings screen.</summary>
        Options,
        /// <summary>Credits / victory screen after level 49.</summary>
        End
    }

    // -------------------------------------------------------------------------
    //  Enemy archetypes
    // -------------------------------------------------------------------------

    /// <summary>
    /// Standard enemy types that appear in normal (non-boss) levels.
    /// </summary>
    public enum EnemyType
    {
        /// <summary>Melee guard with a wide sweep; common in early zones.</summary>
        AshenGuard,
        /// <summary>Floating ranged attacker that orbits the player.</summary>
        EmberWraith,
        /// <summary>Fast, low-to-ground crawler that rushes in packs.</summary>
        BoneCrawler,
        /// <summary>Blocks attacks from the front; must be flanked or slammed.</summary>
        ShieldBearer,
        /// <summary>Slow, tanky; immune to combos unless staggered.</summary>
        StoneGolem
    }

    // -------------------------------------------------------------------------
    //  Boss types – one per zone (10 total)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Zone bosses encountered on levels 4, 9, 14, 19, 24, 29, 34, 39, 44, 49.
    /// Integer value == zone index, matching <see cref="ZoneType"/>.
    /// </summary>
    public enum BossType
    {
        /// <summary>Zone 0 – Level  4  – Ashfields warden.</summary>
        AshenWarden        = 0,
        /// <summary>Zone 1 – Level  9  – Massive armoured titan of the Ember Crypts.</summary>
        ElderTitan         = 1,
        /// <summary>Zone 2 – Level 14  – Draconic aspect born from Shattered Ramparts.</summary>
        DragonAspect       = 2,
        /// <summary>Zone 3 – Level 19  – Undead king risen from the Slag Pits.</summary>
        BoneKing           = 3,
        /// <summary>Zone 4 – Level 24  – Living flood that haunts the Drowned Citadel.</summary>
        TheFlood           = 4,
        /// <summary>Zone 5 – Level 29  – Ancient sand spirit of the Bone Wastes.</summary>
        SandWraith         = 5,
        /// <summary>Zone 6 – Level 34  – Colossal obsidian automaton guarding the Spire.</summary>
        ObsidianSentinel   = 6,
        /// <summary>Zone 7 – Level 39  – Corrupted being that bleeds chaos from The Wound.</summary>
        TheCorruption      = 7,
        /// <summary>Zone 8 – Level 44  – Storm-born wyrm on the Dragon's Approach.</summary>
        StormDrake         = 8,
        /// <summary>Zone 9 – Level 49  – Final boss – the Dark Lord on the Throne of Ash.</summary>
        DarkLordDrakar     = 9
    }

    // -------------------------------------------------------------------------
    //  Player abilities
    // -------------------------------------------------------------------------

    /// <summary>
    /// Learnable special abilities for the Ember Knight.
    /// </summary>
    public enum AbilityType
    {
        /// <summary>Fires a horizontal ember projectile. Costs 15 MP.</summary>
        EmberWave,
        /// <summary>Creates a shield that absorbs 3 hits. Costs 25 MP.</summary>
        FireShield,
        /// <summary>Explosive AOE burst centred on the player. Costs 40 MP.</summary>
        Inferno
    }

    // -------------------------------------------------------------------------
    //  Consumable items
    // -------------------------------------------------------------------------

    /// <summary>
    /// Edible items found throughout levels; each restores health, mana, or both.
    /// </summary>
    public enum FoodType
    {
        /// <summary>Small HP restore. Common drop.</summary>
        EmberBread,
        /// <summary>Medium HP + small MP restore. Zone 1+ drop.</summary>
        AshStew,
        /// <summary>Full MP restore. Shrine vendor item.</summary>
        ManaDraught,
        /// <summary>Full HP + MP restore + brief invincibility. Rare drop.</summary>
        PhoenixElixir,
        /// <summary>Small HP restore, light snack. Common drop.</summary>
        BoneJerky,
        /// <summary>Small MP restore. Grows on special platforms.</summary>
        EmberFruit
    }

    // -------------------------------------------------------------------------
    //  World zones
    // -------------------------------------------------------------------------

    /// <summary>
    /// Each zone spans 5 levels: zone = Mathf.Clamp(level / 5, 0, 9).
    /// Integer value == zone index.
    /// </summary>
    public enum ZoneType
    {
        /// <summary>Zone 0 – Levels  0-4  – Smouldering grasslands.</summary>
        Ashfields          = 0,
        /// <summary>Zone 1 – Levels  5-9  – Underground crypts lit by ember veins.</summary>
        EmberCrypts        = 1,
        /// <summary>Zone 2 – Levels 10-14 – Crumbling fortress walls.</summary>
        ShatteredRamparts  = 2,
        /// <summary>Zone 3 – Levels 15-19 – Molten industrial pits.</summary>
        SlagPits           = 3,
        /// <summary>Zone 4 – Levels 20-24 – Flooded ancient city.</summary>
        DrownedCitadel     = 4,
        /// <summary>Zone 5 – Levels 25-29 – Bleached bone desert.</summary>
        BoneWastes         = 5,
        /// <summary>Zone 6 – Levels 30-34 – Black-glass tower.</summary>
        ObsidianSpire      = 6,
        /// <summary>Zone 7 – Levels 35-39 – Festering rift in reality.</summary>
        TheWound           = 7,
        /// <summary>Zone 8 – Levels 40-44 – Volcanic ascent toward Drakar's peak.</summary>
        DragonsApproach    = 8,
        /// <summary>Zone 9 – Levels 45-49 – Drakar's seat of power.</summary>
        ThroneOfAsh        = 9
    }

    // -------------------------------------------------------------------------
    //  All numeric / string constants
    // -------------------------------------------------------------------------

    /// <summary>
    /// Central repository of every "magic number" used across the project.
    /// Import once, reference everywhere to keep values in sync.
    /// </summary>
    public static class GameConstants
    {
        // --- Level / zone structure ---

        /// <summary>Total number of levels in the game (0-based indices 0-49).</summary>
        public const int TotalLevels           = 50;

        /// <summary>Number of levels per zone before the zone increments.</summary>
        public const int LevelsPerZone         = 5;

        /// <summary>Total number of distinct zones.</summary>
        public const int TotalZones            = 10;

        /// <summary>
        /// Within a zone, the boss is on the last level (index 4).
        /// A level is a boss level when: level % LevelsPerZone == BossLevelOffset.
        /// </summary>
        public const int BossLevelOffset       = 4;

        // --- Difficulty ---

        /// <summary>Difficulty at level 0.</summary>
        public const float BaseDifficulty      = 1.0f;

        /// <summary>Difficulty increases by this amount per level: difficulty = 1 + level * 0.12.</summary>
        public const float DifficultyPerLevel  = 0.12f;

        // --- Player movement ---

        /// <summary>Horizontal movement speed in units/second.</summary>
        public const float PlayerMoveSpeed     = 6.0f;

        /// <summary>Impulse applied on jump.</summary>
        public const float PlayerJumpForce     = 14.0f;

        /// <summary>Seconds after leaving a ledge during which the player may still jump.</summary>
        public const float CoyoteTime          = 0.12f;

        /// <summary>Seconds before landing that a jump input is stored and auto-executed.</summary>
        public const float JumpInputBuffer     = 0.10f;

        /// <summary>Fraction of jump force used for the second (double) jump.</summary>
        public const float DoubleJumpMultiplier = 0.88f;

        /// <summary>Gravity scale applied to the Rigidbody2D while wall-sliding.</summary>
        public const float WallSlideGravityScale = 0.3f;

        // --- Dash ---

        /// <summary>Number of fixed-update frames the dash lasts (player is invincible).</summary>
        public const int DashFrames            = 10;

        /// <summary>Horizontal speed during a dash (overrides normal movement).</summary>
        public const float DashSpeed           = 15.0f;

        /// <summary>Base cooldown between dashes in seconds (may be reduced by upgrades).</summary>
        public const float DashCooldown        = 0.80f;

        // --- Slam attack ---

        /// <summary>Vertical velocity applied when the slam is initiated.</summary>
        public const float SlamVelocityY       = -20.0f;

        /// <summary>Half-width of the slam AOE BoxCast (total width = 3 units).</summary>
        public const float SlamAoeHalfWidth    = 3.0f;

        /// <summary>Damage multiplier applied to the slam AOE hit.</summary>
        public const float SlamDamageMultiplier = 1.5f;

        // --- Combo system ---

        /// <summary>Active frames of hits 1 and 2.</summary>
        public const float ComboHit12Duration  = 0.23f;

        /// <summary>Recovery / cooldown after hits 1 and 2.</summary>
        public const float ComboHit12Cooldown  = 0.37f;

        /// <summary>Active frames of hit 3 (finisher).</summary>
        public const float ComboHit3Duration   = 0.33f;

        /// <summary>Recovery after hit 3.</summary>
        public const float ComboHit3Cooldown   = 0.53f;

        /// <summary>Extra damage multiplier on hit 3.</summary>
        public const float ComboHit3DamageMulti = 1.8f;

        /// <summary>Length of the attack BoxCast in front of the player.</summary>
        public const float ComboHitboxRange    = 2.0f;

        // --- Parry ---

        /// <summary>Enemy startup window (frames 0-4) during which a parry is valid.</summary>
        public const int ParryStartupFrames    = 4;

        /// <summary>Duration in seconds an enemy is stunned after a successful parry.</summary>
        public const float ParryStunDuration   = 0.5f;

        // --- HitStop ---

        /// <summary>Default number of frozen frames per successful attack hit.</summary>
        public const int DefaultHitStopFrames  = 3;

        // --- Soul Blast ---

        /// <summary>Projectile speed of the charged Soul Blast.</summary>
        public const float SoulBlastSpeed      = 18.0f;

        /// <summary>Damage multiplier for the Soul Blast.</summary>
        public const float SoulBlastDamageMulti = 2.5f;

        // --- Ability: Ember Wave ---

        /// <summary>MP cost of Ember Wave.</summary>
        public const float EmberWaveManaCost   = 15.0f;

        /// <summary>Cooldown of Ember Wave in seconds.</summary>
        public const float EmberWaveCooldown   = 1.0f;

        /// <summary>Speed of the Ember Wave projectile in units/second.</summary>
        public const float EmberWaveSpeed      = 12.0f;

        /// <summary>Damage multiplier of the Ember Wave projectile.</summary>
        public const float EmberWaveDamageMulti = 1.2f;

        // --- Ability: Fire Shield ---

        /// <summary>MP cost of Fire Shield.</summary>
        public const float FireShieldManaCost  = 25.0f;

        /// <summary>Cooldown of Fire Shield in seconds.</summary>
        public const float FireShieldCooldown  = 3.0f;

        /// <summary>Number of hits the Fire Shield absorbs before breaking.</summary>
        public const int FireShieldMaxHits     = 3;

        // --- Ability: Inferno ---

        /// <summary>MP cost of Inferno.</summary>
        public const float InfernoManaCost     = 40.0f;

        /// <summary>Cooldown of Inferno in seconds.</summary>
        public const float InfernoCooldown     = 5.0f;

        /// <summary>Width of the Inferno AOE BoxCast in units.</summary>
        public const float InfernoAoeWidth     = 5.0f;

        /// <summary>Height of the Inferno AOE BoxCast in units.</summary>
        public const float InfernoAoeHeight    = 3.0f;

        /// <summary>Damage multiplier of the Inferno AOE.</summary>
        public const float InfernoDamageMulti  = 2.0f;

        // --- Mana ---

        /// <summary>Default maximum mana pool.</summary>
        public const float DefaultMaxMana      = 50.0f;

        /// <summary>Mana regenerated per second during normal play.</summary>
        public const float ManaRegenRate       = 1.2f;

        // --- Ability unlock levels (0-based level index) ---

        /// <summary>Level at which the Dash ability becomes available.</summary>
        public const int DashUnlockLevel       = 5;

        /// <summary>Level at which Wall Jump becomes available.</summary>
        public const int WallJumpUnlockLevel   = 10;

        /// <summary>Level at which the Slam becomes available.</summary>
        public const int SlamUnlockLevel       = 15;

        /// <summary>Level at which Soul Blast becomes available.</summary>
        public const int SoulBlastUnlockLevel  = 20;

        // --- Physics layers (resolve via LayerMask.NameToLayer at runtime) ---

        public const string LayerGround        = "Ground";
        public const string LayerEnemy         = "Enemy";
        public const string LayerPlayer        = "Player";
        public const string LayerProjectile    = "Projectile";
        public const string LayerWall          = "Wall";

        // --- Tags ---

        public const string TagPlayer          = "Player";
        public const string TagEnemy           = "Enemy";
        public const string TagBoss            = "Boss";
        public const string TagGround          = "Ground";
        public const string TagWall            = "Wall";
        public const string TagShrine          = "Shrine";

        // --- Scene names ---

        public const string SceneTitle         = "TitleScreen";
        public const string SceneGame          = "GameScene";
        public const string SceneCredits       = "EndScreen";

        // --- PlayerPrefs / save keys ---

        public const string SaveKeyLevel       = "save_current_level";
        public const string SaveKeyShards      = "save_total_shards";
        public const string SaveKeyOptions     = "save_options";
        public const string SaveKeyAbilities   = "save_abilities";

        // --- Camera shake ---

        /// <summary>Screen shake magnitude triggered by the Slam landing.</summary>
        public const float SlamShakeMagnitude  = 0.35f;

        /// <summary>Screen shake duration for the Slam landing.</summary>
        public const float SlamShakeDuration   = 0.25f;
    }
}
