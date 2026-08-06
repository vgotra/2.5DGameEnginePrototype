using System.Numerics;

namespace Engine.Platform;

public interface IGameWindow : IDisposable
{
    Vector2 Size { get; }
    bool ShouldClose { get; }
    bool Fullscreen { get; }
    bool IsMinimized { get; }
    NativeWindowSurface NativeSurface { get; }
    void PumpEvents();
    void SetFullscreen(bool fullscreen);
    void SetTitle(string title);
    void Close();
}
