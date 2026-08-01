using System.Collections.Generic;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe class DescriptorSetAllocator : IDisposable
{
    private readonly VkDevice _device;
    private readonly VkDeviceApi _deviceApi;
    private readonly VkDescriptorPool _pool;
    private readonly Dictionary<ulong, VkDescriptorSetLayout> _layouts = new();
    private readonly List<VkDescriptorSet> _allocatedSets = new();

    public DescriptorSetAllocator(VkDevice device, VkDeviceApi deviceApi, uint maxSets = 1024)
    {
        _device = device;
        _deviceApi = deviceApi;

        VkDescriptorPoolSize[] poolSizes =
        [
            new() { type = VkDescriptorType.CombinedImageSampler, descriptorCount = maxSets },
            new() { type = VkDescriptorType.UniformBuffer, descriptorCount = maxSets }
        ];

        fixed (VkDescriptorPoolSize* poolSizesPtr = poolSizes)
        {
            VkDescriptorPoolCreateInfo poolInfo = new()
            {
                maxSets = maxSets,
                poolSizeCount = (uint)poolSizes.Length,
                pPoolSizes = poolSizesPtr,
                flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet
            };

            VkResult result = deviceApi.vkCreateDescriptorPool(&poolInfo, null, out _pool);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"Descriptor pool creation failed: {result}");
        }
    }

    public VkDescriptorSetLayout GetLayout(VkDescriptorType type, VkShaderStageFlags stages, uint binding)
    {
        ulong key = ((ulong)type << 48) | ((ulong)stages << 32) | binding;
        if (_layouts.TryGetValue(key, out VkDescriptorSetLayout layout))
            return layout;

        VkDescriptorSetLayoutBinding layoutBinding = new()
        {
            binding = binding,
            descriptorType = type,
            descriptorCount = 1,
            stageFlags = stages
        };

        VkDescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            bindingCount = 1,
            pBindings = &layoutBinding
        };

        VkResult result = _deviceApi.vkCreateDescriptorSetLayout(&layoutInfo, null, out layout);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Descriptor set layout creation failed: {result}");

        _layouts[key] = layout;
        return layout;
    }

    public VkDescriptorSet Allocate(VkDescriptorSetLayout layout)
    {
        VkDescriptorSetAllocateInfo allocInfo = new()
        {
            descriptorPool = _pool,
            descriptorSetCount = 1,
            pSetLayouts = &layout
        };

        VkDescriptorSet set;
        VkResult result = _deviceApi.vkAllocateDescriptorSets(&allocInfo, &set);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"Descriptor set allocation failed: {result}");

        _allocatedSets.Add(set);
        return set;
    }

    public void WriteTexture(VkDescriptorSet set, VkImageView imageView, VkSampler sampler, uint binding = 0)
    {
        VkDescriptorImageInfo imageInfo = new()
        {
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            imageView = imageView,
            sampler = sampler
        };

        VkWriteDescriptorSet write = new()
        {
            dstSet = set,
            dstBinding = binding,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            pImageInfo = &imageInfo
        };

        _deviceApi.vkUpdateDescriptorSets(1, &write, 0, null);
    }

    public void WriteUniformBuffer(VkDescriptorSet set, VkBuffer buffer, nuint offset, nuint range, uint binding = 0)
    {
        VkDescriptorBufferInfo bufferInfo = new()
        {
            buffer = buffer,
            offset = offset,
            range = range
        };

        VkWriteDescriptorSet write = new()
        {
            dstSet = set,
            dstBinding = binding,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.UniformBuffer,
            pBufferInfo = &bufferInfo
        };

        _deviceApi.vkUpdateDescriptorSets(1, &write, 0, null);
    }

    public void Free(VkDescriptorSet set)
    {
        if (_allocatedSets.Remove(set))
        {
            _deviceApi.vkFreeDescriptorSets(_pool, 1, &set);
        }
    }

    public void Dispose()
    {
        foreach (var set in _allocatedSets)
        {
            _deviceApi.vkFreeDescriptorSets(_pool, 1, &set);
        }
        _allocatedSets.Clear();

        foreach (var layout in _layouts.Values)
        {
            if (layout.IsNotNull)
                _deviceApi.vkDestroyDescriptorSetLayout(layout);
        }
        _layouts.Clear();

        if (_pool.IsNotNull)
            _deviceApi.vkDestroyDescriptorPool(_pool);
    }
}