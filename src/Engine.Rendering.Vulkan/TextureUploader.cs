using System.Runtime.InteropServices;
using Engine.Rendering;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe class TextureUploader : IDisposable
{
    private const VkFormat TextureFormat = VkFormat.R8G8B8A8Srgb;

    private readonly VkDevice _device;
    private readonly VkDeviceApi _deviceApi;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkPhysicalDeviceMemoryProperties _memoryProperties;
    private readonly VkQueue _graphicsQueue;
    private readonly VkCommandPool _commandPool;
    private readonly DescriptorSetAllocator _descriptorAllocator;
    private readonly OneShotCommandBuffer _oneShot;

    private readonly List<TextureResource> _textures = new();

    public TextureUploader(
        VkDevice device,
        VkDeviceApi deviceApi,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        VkQueue graphicsQueue,
        VkCommandPool commandPool,
        DescriptorSetAllocator descriptorAllocator)
    {
        _device = device;
        _deviceApi = deviceApi;
        _physicalDevice = physicalDevice;
        _memoryProperties = memoryProperties;
        _graphicsQueue = graphicsQueue;
        _commandPool = commandPool;
        _descriptorAllocator = descriptorAllocator;
        _oneShot = new OneShotCommandBuffer(deviceApi, commandPool, graphicsQueue);

        UploadTexture([byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue], 1, 1);
    }

    public TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter = TextureFilter.Linear)
    {
        if (rgba.Length != width * height * 4)
            throw new ArgumentException("RGBA data size does not match width*height*4");

        VkImage image = VulkanImage.Create(_device, _deviceApi, width, height, TextureFormat, VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled);
        VkDeviceMemory memory = AllocateImageMemory(image);
        _deviceApi.vkBindImageMemory(image, memory, 0);

        VkImageView imageView = VulkanImage.CreateView(_deviceApi, image, TextureFormat);
        VkSampler sampler = CreateSampler(filter);

        UploadImageData(rgba, width, height, image);

        VkDescriptorSetLayout layout = _descriptorAllocator.GetLayout(
            VkDescriptorType.CombinedImageSampler,
            VkShaderStageFlags.Fragment,
            0);

        VkDescriptorSet descriptorSet = _descriptorAllocator.Allocate(layout);
        _descriptorAllocator.WriteTexture(descriptorSet, imageView, sampler, 0);

        var resource = new TextureResource
        {
            Image = image,
            Memory = memory,
            ImageView = imageView,
            Sampler = sampler,
            DescriptorSet = descriptorSet,
            Width = width,
            Height = height
        };

        int handle = _textures.Count;
        _textures.Add(resource);

        return new TextureHandle(handle);
    }

    public VkDescriptorSet GetDescriptorSet(TextureHandle handle)
    {
        if (handle.Value < 0 || handle.Value >= _textures.Count)
            return VkDescriptorSet.Null;
        return _textures[handle.Value].DescriptorSet;
    }

    public void Dispose()
    {
        for (int i = _textures.Count - 1; i >= 0; i--)
        {
            var tex = _textures[i];
            if (tex.Sampler.IsNotNull) _deviceApi.vkDestroySampler(tex.Sampler);
            if (tex.ImageView.IsNotNull) _deviceApi.vkDestroyImageView(tex.ImageView);
            if (tex.Image.IsNotNull) _deviceApi.vkDestroyImage(tex.Image);
            if (tex.Memory.IsNotNull) _deviceApi.vkFreeMemory(tex.Memory);
            _descriptorAllocator.Free(tex.DescriptorSet);
        }
        _textures.Clear();
        _oneShot.Dispose();
    }

    private VkDeviceMemory AllocateImageMemory(VkImage image)
    {
        VkMemoryRequirements memReqs;
        _deviceApi.vkGetImageMemoryRequirements(image, &memReqs);

        uint memoryTypeIndex = VulkanMemory.FindMemoryType(_memoryProperties, memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        VkMemoryAllocateInfo allocInfo = new()
        {
            allocationSize = memReqs.size,
            memoryTypeIndex = memoryTypeIndex
        };

        VkResult result = _deviceApi.vkAllocateMemory(&allocInfo, null, out VkDeviceMemory memory);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Image memory allocation failed: {result}");

        return memory;
    }

    private VkSampler CreateSampler(TextureFilter filter)
    {
        VkFilter vulkanFilter = filter == TextureFilter.Nearest ? VkFilter.Nearest : VkFilter.Linear;
        VkSamplerCreateInfo samplerInfo = new()
        {
            magFilter = vulkanFilter,
            minFilter = vulkanFilter,
            addressModeU = VkSamplerAddressMode.Repeat,
            addressModeV = VkSamplerAddressMode.Repeat,
            addressModeW = VkSamplerAddressMode.Repeat,
            anisotropyEnable = false,
            maxAnisotropy = 1f,
            borderColor = VkBorderColor.IntOpaqueBlack,
            unnormalizedCoordinates = false,
            compareEnable = false,
            compareOp = VkCompareOp.Always,
            mipmapMode = VkSamplerMipmapMode.Linear,
            mipLodBias = 0f,
            minLod = 0f,
            maxLod = 0f
        };

        VkResult result = _deviceApi.vkCreateSampler(&samplerInfo, null, out VkSampler sampler);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Sampler creation failed: {result}");

        return sampler;
    }

    private void UploadImageData(ReadOnlySpan<byte> rgba, int width, int height, VkImage image)
    {
        nuint bufferSize = (nuint)(width * height * 4);
        VulkanBuffer stagingBuffer = VulkanBuffer.CreateStagingBuffer(
            _device, _deviceApi, _physicalDevice, _memoryProperties, bufferSize);

        fixed (byte* rgbaPointer = rgba)
            stagingBuffer.UploadData(rgbaPointer, bufferSize);

        VkCommandBuffer cmdBuffer = _oneShot.Begin();

        VulkanImage.TransitionLayout(_deviceApi, cmdBuffer, image,
            VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
            VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer,
            0, VkAccessFlags.TransferWrite);

        VkBufferImageCopy region = new()
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            },
            imageOffset = new VkOffset3D { x = 0, y = 0, z = 0 },
            imageExtent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 }
        };

        _deviceApi.vkCmdCopyBufferToImage(cmdBuffer, stagingBuffer.Buffer, image, VkImageLayout.TransferDstOptimal, 1, &region);

        VulkanImage.TransitionLayout(_deviceApi, cmdBuffer, image,
            VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal,
            VkPipelineStageFlags.Transfer, VkPipelineStageFlags.FragmentShader,
            VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead);

        _oneShot.Submit(cmdBuffer);

        stagingBuffer.Dispose();
    }

    private struct TextureResource
    {
        public VkImage Image;
        public VkDeviceMemory Memory;
        public VkImageView ImageView;
        public VkSampler Sampler;
        public VkDescriptorSet DescriptorSet;
        public int Width;
        public int Height;
    }
}
