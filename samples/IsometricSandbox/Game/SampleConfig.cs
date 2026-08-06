namespace IsometricSandbox.Game;

// Tune the sample here — window, player, and world knobs.
// Changing these is the supported way to modify the demo.
public static class SampleConfig
{
    public const string WindowTitle = "Archer in the Forest";
    public const int WindowWidth = 800;
    public const int WindowHeight = 600;

    // Deer/rabbits alive at once. ArcherGame.MaxAnimals is the enforced cap.
    public const int AnimalCount = ArcherGame.MaxAnimals;

    public const float PlayerSpeed = 4f;
    public const float PlayerRadius = 0.2f;
    public const float JumpDuration = 0.24f;

    // Render lift of the player while jumping, in screen pixels.
    public const float JumpHeight = 18f;

    // Minimum time the splash screen stays visible during startup, in seconds.
    public const float SplashMinimumSeconds = 0.8f;
}
