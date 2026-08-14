namespace Engine.Rendering.Vulkan;

public static class TextureUploadLimits
{
    public const int PendingRequests = 256;
    public const int InFlightBatches = 3;
    public const int DescriptorCapacity = 1024;
}

/// <summary>Render-thread upload lifecycle. A handle is published only in <see cref="Completed"/>.</summary>
public enum TextureUploadState : byte
{
    Invalid,
    Queued,
    Submitted,
    Completed,
    Failed
}

/// <summary>Opaque Vulkan upload request identifier. Queue capacity is 256 requests and three batches may be in flight.</summary>
public readonly record struct TextureUploadTicket(int Value)
{
    public bool IsValid => Value >= 0;
    public static TextureUploadTicket Invalid => new(-1);
}

/// <summary>Allocation-free snapshot of upload and descriptor activity.</summary>
public readonly record struct TextureUploadDiagnostics(
    DescriptorMode DescriptorMode,
    int PendingUploadCount,
    int InFlightBatchCount,
    long CompletedUploadCount,
    long FailedUploadCount,
    long PendingUploadBytes,
    int ResidentTextureCount,
    int DescriptorCapacity,
    int DescriptorHighWaterMark,
    long FallbackDescriptorBinds,
    long IndexedDescriptorBinds,
    double MaxUploadLatencyMs);
