using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal static class VulkanMemory
{
    public static uint FindMemoryType(
        VkPhysicalDeviceMemoryProperties memoryProperties,
        uint typeFilter,
        VkMemoryPropertyFlags flags)
    {
        for (uint i = 0; i < memoryProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 &&
                (memoryProperties.memoryTypes[(int)i].propertyFlags & flags) == flags)
            {
                return i;
            }
        }
        throw new InvalidOperationException("Failed to find suitable memory type");
    }
}
