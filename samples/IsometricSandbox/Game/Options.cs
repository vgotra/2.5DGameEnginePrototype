namespace IsometricSandbox.Game;

// Command-line options for the sample. Unknown flags are ignored so the
// program still runs when extras are passed (e.g. via `dotnet run --`).
public sealed record Options(bool FlatMode, bool StartFullscreen, bool ShowMetrics, double FrameCap)
{
    public static Options Parse(string[] args)
    {
        bool flatMode = args.Contains("--2d");
        bool startFullscreen = args.Contains("--fullscreen");
        bool showMetrics = args.Contains("--metrics");
        return new Options(flatMode, startFullscreen, showMetrics, ReadFrameCap(args));
    }

    private static double ReadFrameCap(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--cap" && double.TryParse(args[i + 1], out double fps)) return fps;
        }
        return 0;
    }
}
