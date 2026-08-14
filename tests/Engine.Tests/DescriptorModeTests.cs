using Engine.Rendering.Vulkan;

namespace Engine.Tests;

internal static class DescriptorModeTests
{
    public static readonly TestCase[] Tests =
    [
        new(nameof(AutoWithoutFeaturesUsesFallback), AutoWithoutFeaturesUsesFallback),
        new(nameof(AutoWithFeaturesUsesIndexed), AutoWithFeaturesUsesIndexed),
        new(nameof(ForcedFallbackWins), ForcedFallbackWins),
        new(nameof(ForcedIndexedWithoutFeaturesFails), ForcedIndexedWithoutFeaturesFails)
    ];

    private static readonly DescriptorCapabilities Supported = new(true, true, true, true, true);
    private static readonly DescriptorCapabilities Unsupported = default;

    private static void AutoWithoutFeaturesUsesFallback()
        => TestAssert.True(DescriptorModeSelector.Select(DescriptorModeOverride.Auto, in Unsupported) == DescriptorMode.PerTextureSets, "auto fallback");

    private static void AutoWithFeaturesUsesIndexed()
        => TestAssert.True(DescriptorModeSelector.Select(DescriptorModeOverride.Auto, in Supported) == DescriptorMode.IndexedArray, "auto indexed");

    private static void ForcedFallbackWins()
        => TestAssert.True(DescriptorModeSelector.Select(DescriptorModeOverride.Fallback, in Supported) == DescriptorMode.PerTextureSets, "forced fallback");

    private static void ForcedIndexedWithoutFeaturesFails()
    {
        bool failed = false;
        try { DescriptorModeSelector.Select(DescriptorModeOverride.Indexed, in Unsupported); }
        catch (InvalidOperationException) { failed = true; }
        TestAssert.True(failed, "forced indexed rejects unsupported device");
    }
}
