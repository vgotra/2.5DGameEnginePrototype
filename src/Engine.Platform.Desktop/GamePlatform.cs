using Engine.Platform.Win32;

namespace Engine.Platform.Desktop;

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
