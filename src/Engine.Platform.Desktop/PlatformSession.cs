namespace Engine.Platform.Desktop;

internal sealed class PlatformSession : IDisposable
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
