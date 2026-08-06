using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public static unsafe class VulkanImage
{
    public static VkImage Create(
        VkDevice device,
        VkDeviceApi deviceApi,
        int width,
        int height,
        VkFormat format,
        VkImageUsageFlags usage)
    {
        VkImageCreateInfo imageInfo = new()
        {
            imageType = VkImageType.Image2D,
            format = format,
            extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined
        };

        VkResult result = deviceApi.vkCreateImage(&imageInfo, null, out VkImage image);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Image creation failed: {result}");

        return image;
    }

    public static VkImageView CreateView(VkDeviceApi deviceApi, VkImage image, VkFormat format)
    {
        VkImageViewCreateInfo viewInfo = new()
        {
            image = image,
            viewType = VkImageViewType.Image2D,
            format = format,
            components = VkComponentMapping.Identity,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };

        VkResult result = deviceApi.vkCreateImageView(&viewInfo, null, out VkImageView view);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Image view creation failed: {result}");

        return view;
    }

    public static void TransitionLayout(
        VkDeviceApi deviceApi,
        VkCommandBuffer cmdBuffer,
        VkImage image,
        VkImageLayout oldLayout,
        VkImageLayout newLayout,
        VkPipelineStageFlags srcStage,
        VkPipelineStageFlags dstStage,
        VkAccessFlags srcAccess,
        VkAccessFlags dstAccess)
    {
        VkImageMemoryBarrier barrier = new()
        {
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = uint.MaxValue,
            dstQueueFamilyIndex = uint.MaxValue,
            image = image,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            },
            srcAccessMask = srcAccess,
            dstAccessMask = dstAccess
        };

        deviceApi.vkCmdPipelineBarrier(cmdBuffer,
            srcStage,
            dstStage,
            VkDependencyFlags.None,
            0, null, 0, null, 1, &barrier);
    }
}
