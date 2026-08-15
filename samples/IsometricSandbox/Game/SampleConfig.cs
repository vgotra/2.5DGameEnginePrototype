using Engine.App;

namespace IsometricSandbox.Game;

// Tune the sample here — window, player, world, and simulation knobs.
// Changing these is the supported way to modify the demo.
public static class SampleConfig
{
    public const string WindowTitle = "Archer in the Forest";
    public const int WindowWidth = 800;
    public const int WindowHeight = 600;
    public const float PlayerSpriteHeight = 56f;
    public const float SplashTitleMaxScale = 3f;
    public const float SplashPercentageMaxScale = 2f;
    public const float SplashBarMaxWidth = 480f;
    public const float SplashTextWidthMultiplier = 1.3f;
    public const int NpcParallelThreshold = 64;

    public const int MaxEnemies = 10;

    public const float PlayerSpeed = 7f;
    public const float PlayerRadius = 0.2f;
    public const float JumpDuration = 0.24f;

    // Render lift of the player while jumping, in screen pixels.
    public const float JumpHeight = 18f;

    // Minimum time the splash screen stays visible during startup, in seconds.
    public const float SplashMinimumSeconds = 1f;

    // Weapon and projectile tuning.
    public const int MaxArrows = 32;
    public const int ProjectileParallelThreshold = 64;
    public const float ArrowSpeed = 14f;
    public const float ArrowLifetime = 1.5f;
    public const float ArrowRadius = 0.15f;
    public const float HomingRadius = 5f;

    public const float FleeRadius = 3.5f;

    public const int NormalSpriteCapacity = 1024;
    public const int ArpgSpriteCapacity = 1024;
}
