namespace Engine.Platform;

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
