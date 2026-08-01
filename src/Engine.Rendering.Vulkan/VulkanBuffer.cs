using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe struct VulkanBuffer : IDisposable
{
    public VkBuffer Buffer;
    public VkDeviceMemory Memory;
    public nuint Size;
    private VkDevice _device;
    private VkDeviceApi _deviceApi;

    public static VulkanBuffer CreateVertexBuffer(
        VkDevice device,
        VkDeviceApi api,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        nuint size,
        VkBufferUsageFlags usage = VkBufferUsageFlags.VertexBuffer)
    {
        return CreateBuffer(device, api, physicalDevice, memoryProperties, size,
            usage | VkBufferUsageFlags.TransferDst,
            VkMemoryPropertyFlags.DeviceLocal);
    }

    public static VulkanBuffer CreateIndexBuffer(
        VkDevice device,
        VkDeviceApi api,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        nuint size,
        VkBufferUsageFlags usage = VkBufferUsageFlags.IndexBuffer)
    {
        return CreateBuffer(device, api, physicalDevice, memoryProperties, size,
            usage | VkBufferUsageFlags.TransferDst,
            VkMemoryPropertyFlags.DeviceLocal);
    }

    public static VulkanBuffer CreateStagingBuffer(
        VkDevice device,
        VkDeviceApi api,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        nuint size)
    {
        return CreateBuffer(device, api, physicalDevice, memoryProperties, size,
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
    }

    private static VulkanBuffer CreateBuffer(
        VkDevice device,
        VkDeviceApi api,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        nuint size,
        VkBufferUsageFlags usage,
        VkMemoryPropertyFlags memoryFlags)
    {
        VkBufferCreateInfo bufferInfo = new()
        {
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };

        VkResult result = api.vkCreateBuffer(&bufferInfo, null, out VkBuffer buffer);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Buffer creation failed: {result}");

        VkMemoryRequirements memReqs;
        api.vkGetBufferMemoryRequirements(buffer, &memReqs);

        uint memoryTypeIndex = FindMemoryType(memoryProperties, memReqs.memoryTypeBits, memoryFlags);

        VkMemoryAllocateInfo allocInfo = new()
        {
            allocationSize = memReqs.size,
            memoryTypeIndex = memoryTypeIndex
        };

        result = api.vkAllocateMemory(&allocInfo, null, out VkDeviceMemory memory);
        if (result != VkResult.Success)
        {
            api.vkDestroyBuffer(buffer);
            throw new InvalidOperationException($"Buffer memory allocation failed: {result}");
        }

        result = api.vkBindBufferMemory(buffer, memory, 0);
        if (result != VkResult.Success)
        {
            api.vkFreeMemory(memory);
            api.vkDestroyBuffer(buffer);
            throw new InvalidOperationException($"Buffer memory bind failed: {result}");
        }

        return new VulkanBuffer
        {
            Buffer = buffer,
            Memory = memory,
            Size = (nuint)memReqs.size,
            _device = device,
            _deviceApi = api
        };
    }

    private static uint FindMemoryType(
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

    public void UploadData<T>(T[] data) where T : unmanaged
    {
        nuint size = (nuint)(data.Length * Unsafe.SizeOf<T>());
        if (size > Size)
            throw new InvalidOperationException("Data size exceeds buffer size");

        void* mapped;
        VkResult result = _deviceApi.vkMapMemory(Memory, 0, size, 0, &mapped);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Buffer map failed: {result}");

        fixed (T* src = data)
        {
            global::System.Buffer.MemoryCopy(src, mapped, (long)size, (long)size);
        }

        _deviceApi.vkUnmapMemory(Memory);
    }

    public void UploadData(void* data, nuint size)
    {
        if (size > Size)
            throw new InvalidOperationException("Data size exceeds buffer size");

        void* mapped;
        VkResult result = _deviceApi.vkMapMemory(Memory, 0, size, 0, &mapped);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Buffer map failed: {result}");

        global::System.Buffer.MemoryCopy(data, mapped, (long)size, (long)size);
        _deviceApi.vkUnmapMemory(Memory);
    }

    public void Dispose()
    {
        if (Buffer.IsNotNull && _device.IsNotNull)
            _deviceApi.vkDestroyBuffer(Buffer);
        if (Memory.IsNotNull && _device.IsNotNull)
            _deviceApi.vkFreeMemory(Memory);
        Buffer = VkBuffer.Null;
        Memory = VkDeviceMemory.Null;
        Size = 0;
    }
}