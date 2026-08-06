namespace Engine.Platform;

public interface IVulkanSurfaceFactory
{
    string[] RequiredInstanceExtensions { get; }
    nint CreateSurface(nint instanceHandle);
    void DestroySurface(nint instanceHandle, nint surfaceHandle);
}
