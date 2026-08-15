namespace IsometricSandbox.Game;

// Command-line options for the sample. Unknown flags are ignored so the
// program still runs when extras are passed (e.g. via `dotnet run --`).
public sealed record Options(bool StartFullscreen, bool ShowMetrics, bool ForceParallel, bool Simulation, bool Arpg, bool GameplayScenario, int? FrameLimit, double FrameCap)
{
    public static Options Parse(string[] args)
    {
        bool startFullscreen = args.Contains("--fullscreen");
        bool showMetrics = args.Contains("--metrics");
        bool forceParallel = args.Contains("--parallel");
        bool simulation = args.Contains("--simulation");
        bool arpg = args.Contains("--arpg");
        bool gameplayScenario = args.Contains("--phase1") || args.Contains("--arpg-sample");
        return new Options(startFullscreen, showMetrics, forceParallel, simulation, arpg, gameplayScenario, ReadFrameLimit(args), ReadFrameCap(args));
    }

    private static int? ReadFrameLimit(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--frames" && int.TryParse(args[i + 1], out int frames) && frames > 0) return frames;
        return null;
    }

    private static double ReadFrameCap(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--cap" && double.TryParse(args[i + 1], out double fps) && fps >= 0) return fps;
        }
        return 120;
    }
}
