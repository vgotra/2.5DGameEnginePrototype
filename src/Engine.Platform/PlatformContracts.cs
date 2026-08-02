using System.Numerics;

namespace Engine.Platform;

/// <summary>Operating-system windowing backends the engine can target.</summary>
public enum PlatformKind
{
    /// <summary>Microsoft Windows (HWND + HINSTANCE).</summary>
    Win32,
    /// <summary>X11 via XCB (Display* + Window). Planned.</summary>
    X11,
    /// <summary>Wayland (wl_display* + wl_surface*). Planned.</summary>
    Wayland,
    /// <summary>macOS (NSWindow/NSView via MoltenVK). Planned.</summary>
    MacOs
}

/// <summary>
/// Platform-neutral description of the native handles a window exposes to backends such as
/// the Vulkan renderer. Backends use <see cref="Kind"/> to select the correct surface
/// extension and interpret the handle fields.
/// </summary>
public readonly struct NativeWindowSurface
{
    public PlatformKind Kind { get; }
    /// <summary>HWND (Win32), Window (X11), wl_surface* (Wayland), NSView* (macOS).</summary>
    public nint WindowHandle { get; }
    /// <summary>Display* (X11), wl_display* (Wayland). Zero when the platform has no display handle.</summary>
    public nint DisplayHandle { get; }
    /// <summary>HINSTANCE (Win32). Zero on other platforms.</summary>
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
    /// <summary>Native handles required by backends (e.g. Vulkan surface creation).</summary>
    NativeWindowSurface NativeSurface { get; }
    void PumpEvents();
    void SetFullscreen(bool fullscreen);
    void SetTitle(string title);
    void Close();
}
