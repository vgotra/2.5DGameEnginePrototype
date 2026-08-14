using Engine.Rendering.Vulkan;

namespace Engine.Tests;

internal static class TextureUploadPolicyTests
{
    public static readonly TestCase[] Tests =
    [
        new(nameof(InvalidTicketIsNotValid), InvalidTicketIsNotValid),
        new(nameof(ValidTicketIsOpaque), ValidTicketIsOpaque),
        new(nameof(LimitsMatchRuntimePolicy), LimitsMatchRuntimePolicy),
        new(nameof(StatesPreserveLifecycleOrder), StatesPreserveLifecycleOrder)
    ];

    private static void InvalidTicketIsNotValid()
        => TestAssert.True(!TextureUploadTicket.Invalid.IsValid, "invalid ticket is not valid");

    private static void ValidTicketIsOpaque()
        => TestAssert.True(new TextureUploadTicket(17).IsValid && new TextureUploadTicket(17).Value == 17, "valid ticket preserves id");

    private static void LimitsMatchRuntimePolicy()
        => TestAssert.True(TextureUploadLimits.PendingRequests == 256 && TextureUploadLimits.InFlightBatches == 3 && TextureUploadLimits.DescriptorCapacity == 1024, "upload limits");

    private static void StatesPreserveLifecycleOrder()
        => TestAssert.True(TextureUploadState.Invalid < TextureUploadState.Queued && TextureUploadState.Queued < TextureUploadState.Submitted && TextureUploadState.Submitted < TextureUploadState.Completed, "upload lifecycle order");
}
