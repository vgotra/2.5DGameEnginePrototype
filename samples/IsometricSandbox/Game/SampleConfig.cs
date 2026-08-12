namespace IsometricSandbox.Game;

// Tune the sample here — window, player, world, and simulation knobs.
// Changing these is the supported way to modify the demo.
public static class SampleConfig
{
    public const string WindowTitle = "Archer in the Forest";
    public const int WindowWidth = 800;
    public const int WindowHeight = 600;

    // Deer/rabbits alive at once (normal mode).
    public const int MaxAnimals = 10;
    public const int AnimalCount = MaxAnimals;

    public const float PlayerSpeed = 7f;
    public const float PlayerRadius = 0.2f;
    public const float JumpDuration = 0.24f;

    // Render lift of the player while jumping, in screen pixels.
    public const float JumpHeight = 18f;

    // Minimum time the splash screen stays visible during startup, in seconds.
    public const float SplashMinimumSeconds = 0.8f;

    // Weapon and projectile tuning.
    public const int MaxArrows = 32;
    public const float ArrowSpeed = 14f;
    public const float ArrowLifetime = 1.5f;
    public const float ArrowRadius = 0.15f;
    public const float HomingRadius = 5f;

    // Critter behavior.
    public const float FleeRadius = 3.5f;
    public const float DeerSpeed = 1.4f;
    public const float DeerRadius = 0.5f;
    public const float RabbitSpeed = 2.2f;
    public const float RabbitRadius = 0.35f;

    // Sprite buffer sizing (capacity is enforced per run mode).
    public const int NormalSpriteCapacity = 1024;
    public const int SimulationSpriteCapacity = 262_144;

    // Simulation mode: an open stress-test map with a large critter herd.
    public const int SimulationWidth = 128;
    public const int SimulationHeight = 128;
    public const int SimulationCritters = 100_000;
    public const int SimulationFrames = 10;
}
