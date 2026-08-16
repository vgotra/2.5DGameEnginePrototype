using Engine.Platform.SDL3;
using Engine.App;

namespace Engine.Platform.Desktop;

public static class GamePlatform
{
    public static IGameApplicationBackend CreateBackend(GameApplicationOptions options)
        => DesktopGameBackend.Create(options);

    public static GameApplication CreateApplication(GameApplicationOptions options)
    {
        IGameApplicationBackend backend = CreateBackend(options);
        return new GameApplication(options, backend);
    }

    internal static PlatformSession CreateWindow(string title, int width, int height)
    {
        SdlWindow window = new(width, height, title);
        return new PlatformSession(window, new SdlInput(window));
    }
}
