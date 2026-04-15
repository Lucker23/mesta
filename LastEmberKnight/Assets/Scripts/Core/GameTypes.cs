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

}
