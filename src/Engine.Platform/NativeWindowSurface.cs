namespace Engine.Platform;

internal readonly struct NativeWindowSurface(PlatformKind kind, nint windowHandle, nint displayHandle = 0, nint moduleHandle = 0, IVulkanSurfaceFactory? surfaceFactory = null)
{
    public PlatformKind Kind { get; } = kind;
    public nint WindowHandle { get; } = windowHandle;
    public nint DisplayHandle { get; } = displayHandle;
    public nint ModuleHandle { get; } = moduleHandle;
    public IVulkanSurfaceFactory? SurfaceFactory { get; } = surfaceFactory;
}
