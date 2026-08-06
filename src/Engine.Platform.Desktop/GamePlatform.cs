using Engine.Platform.SDL3;

namespace Engine.Platform.Desktop;

public static class GamePlatform
{
    public static PlatformSession CreateWindow(string title, int width, int height)
    {
        SdlWindow window = new(width, height, title);
        return new PlatformSession(window, new SdlInput(window));
    }
}
