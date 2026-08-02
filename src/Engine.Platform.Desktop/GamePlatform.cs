using Engine.Platform.Win32;

namespace Engine.Platform.Desktop;

/// <summary>Holds the window and input state for one desktop session.</summary>
public sealed class PlatformSession : IDisposable
{
    public IGameWindow Window { get; }
    public IInputState Input { get; }

    internal PlatformSession(IGameWindow window, IInputState input)
    {
        Window = window;
        Input = input;
    }

    public void Dispose() => Window.Dispose();
}

/// <summary>
/// Creates platform backends for the current operating system. This is the seam where new
/// desktop backends (Linux via SDL2, macOS) are registered without touching the sample or
/// gameplay code; see <c>docs/LinuxSupportPlan.md</c>.
/// </summary>
public static class GamePlatform
{
    public static PlatformSession CreateWindow(string title, int width, int height)
    {
        if (OperatingSystem.IsWindows())
        {
            Win32Window window = new(width, height, title);
            return new PlatformSession(window, new Win32Input(window.NativeSurface.WindowHandle));
        }

        throw new PlatformNotSupportedException(
            "Only Windows is supported today. Linux (via SDL2) is planned; see docs/LinuxSupportPlan.md.");
    }
}
