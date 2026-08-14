namespace Engine.Rendering.Vulkan;

public enum DescriptorMode : byte
{
    PerTextureSets,
    IndexedArray
}

/// <summary>Startup selection policy. Indexed mode throws during initialization when required device features are unavailable.</summary>
public enum DescriptorModeOverride : byte
{
    Auto,
    Fallback,
    Indexed
}

public readonly record struct DescriptorCapabilities(
    bool RuntimeDescriptorArray,
    bool SampledImageArrayNonUniformIndexing,
    bool PartiallyBound,
    bool VariableDescriptorCount,
    bool UpdateAfterBind)
{
    public bool SupportsIndexedArray => RuntimeDescriptorArray &&
        SampledImageArrayNonUniformIndexing && PartiallyBound &&
        VariableDescriptorCount && UpdateAfterBind;
}

public static class DescriptorModeSelector
{
    public static DescriptorMode Select(DescriptorModeOverride mode, in DescriptorCapabilities capabilities)
    {
        if (mode == DescriptorModeOverride.Fallback) return DescriptorMode.PerTextureSets;
        if (mode == DescriptorModeOverride.Indexed && !capabilities.SupportsIndexedArray)
            throw new InvalidOperationException("Indexed descriptor mode was requested but the Vulkan device lacks required descriptor-indexing features.");
        return mode == DescriptorModeOverride.Indexed || capabilities.SupportsIndexedArray
            ? DescriptorMode.IndexedArray
            : DescriptorMode.PerTextureSets;
    }
}
