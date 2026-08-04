using System.Numerics;

namespace Engine.Platform;

public enum PlatformKind
{
    Win32,
    X11,
    Wayland,
    MacOs
}

public readonly struct NativeWindowSurface
{
    public PlatformKind Kind { get; }
    public nint WindowHandle { get; }
    public nint DisplayHandle { get; }
    public nint ModuleHandle { get; }

    public NativeWindowSurface(PlatformKind kind, nint windowHandle, nint displayHandle = 0, nint moduleHandle = 0)
    {
        Kind = kind;
        WindowHandle = windowHandle;
        DisplayHandle = displayHandle;
        ModuleHandle = moduleHandle;
    }
}

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
