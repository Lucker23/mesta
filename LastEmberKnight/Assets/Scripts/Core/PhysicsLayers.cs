namespace LastEmberKnight
{
    /// <summary>
    /// Centralised layer index constants matching TagManager.asset.
    /// Use LayerMask.GetMask(PhysicsLayers.GroundName) for masks, or the int
    /// constants for LayerMask operations in Physics2D calls.
    /// </summary>
    public static class PhysicsLayers
    {
        public const string DefaultName         = "Default";
        public const string BackgroundName      = "Background";
        public const string ForegroundName      = "Foreground";
        public const string PlayerName          = "Player";
        public const string EnemyName           = "Enemy";
        public const string PlayerProjectileName= "PlayerProjectile";
        public const string EnemyProjectileName = "EnemyProjectile";
        public const string GroundName          = "Ground";
        public const string OneWayPlatformName  = "OneWayPlatform";
        public const string TriggerName         = "Trigger";

        // Integer layer indices (must match TagManager)
        public const int Default          = 0;
        public const int Background       = 6;
        public const int Foreground       = 7;
        public const int Player           = 8;
        public const int Enemy            = 9;
        public const int PlayerProjectile = 10;
        public const int EnemyProjectile  = 11;
        public const int Ground           = 12;
        public const int OneWayPlatform   = 13;
        public const int Trigger          = 14;

        // Useful composite masks
        public static readonly int GroundMask        = (1 << Ground) | (1 << OneWayPlatform);
        public static readonly int EnemyMask         = 1 << Enemy;
        public static readonly int PlayerMask        = 1 << Player;
        public static readonly int AllCharacterMask  = (1 << Player) | (1 << Enemy);
        public static readonly int ProjectileMask    = (1 << PlayerProjectile) | (1 << EnemyProjectile);
    }

    /// <summary>
    /// Sorting layer name constants matching TagManager sorting layers.
    /// </summary>
    public static class SortingLayers
    {
        public const string Background    = "Background";
        public const string FarBackground = "FarBackground";
        public const string MidBackground = "MidBackground";
        public const string NearBackground= "NearBackground";
        public const string Platforms     = "Platforms";
        public const string Default       = "Default";
        public const string Characters    = "Characters";
        public const string Projectiles   = "Projectiles";
        public const string Foreground    = "Foreground";
        public const string UI            = "UI";
        public const string Effects       = "Effects";
    }
}
