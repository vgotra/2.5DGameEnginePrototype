namespace Engine.Platform;

internal interface IVulkanSurfaceFactory
{
    string[] RequiredInstanceExtensions { get; }
    nint CreateSurface(nint instanceHandle);
    void DestroySurface(nint instanceHandle, nint surfaceHandle);
}
