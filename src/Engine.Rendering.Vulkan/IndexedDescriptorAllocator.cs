using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal unsafe sealed class IndexedDescriptorAllocator : IDisposable
{
    // Indices are stable for resident textures; this milestone does not evict or reuse them.
    public const uint Capacity = TextureUploadLimits.DescriptorCapacity;
    private readonly VkDeviceApi _api;
    private readonly VkDescriptorPool _pool;
    private readonly VkDescriptorSetLayout _layout;
    private VkDescriptorSet _set;
    private uint _nextIndex;
    private readonly Stack<uint> _releasedIndices = new();

    public VkDescriptorSetLayout Layout => _layout;
    public VkDescriptorSet Set => _set;

    public IndexedDescriptorAllocator(VkDevice device, VkDeviceApi api)
    {
        _api = api;
        VkDescriptorPoolSize poolSize = new() { type = VkDescriptorType.CombinedImageSampler, descriptorCount = Capacity };
        VkDescriptorPoolCreateInfo poolInfo = new()
        {
            flags = VkDescriptorPoolCreateFlags.UpdateAfterBind,
            maxSets = 1,
            poolSizeCount = 1,
            pPoolSizes = &poolSize
        };
        VkResult result = _api.vkCreateDescriptorPool(&poolInfo, null, out _pool);
        if (result != VkResult.Success) throw new InvalidOperationException($"Indexed descriptor pool creation failed: {result}");

        VkDescriptorSetLayoutBinding binding = new()
        {
            binding = 0,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = Capacity,
            stageFlags = VkShaderStageFlags.Fragment
        };
        VkDescriptorBindingFlags flags = VkDescriptorBindingFlags.UpdateAfterBind | VkDescriptorBindingFlags.PartiallyBound | VkDescriptorBindingFlags.VariableDescriptorCount;
        VkDescriptorSetLayoutBindingFlagsCreateInfo flagsInfo = new()
        {
            sType = VkStructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
            bindingCount = 1,
            pBindingFlags = &flags
        };
        VkDescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            sType = VkStructureType.DescriptorSetLayoutCreateInfo,
            pNext = &flagsInfo,
            flags = VkDescriptorSetLayoutCreateFlags.UpdateAfterBindPool,
            bindingCount = 1,
            pBindings = &binding
        };
        result = _api.vkCreateDescriptorSetLayout(&layoutInfo, null, out _layout);
        if (result != VkResult.Success) throw new InvalidOperationException($"Indexed descriptor layout creation failed: {result}");

        uint count = Capacity;
        VkDescriptorSetLayout layoutHandle = _layout;
        VkDescriptorSetAllocateInfo allocateInfo = new()
        {
            descriptorPool = _pool,
            descriptorSetCount = 1,
            pSetLayouts = &layoutHandle
        };
        VkDescriptorSetVariableDescriptorCountAllocateInfo variableInfo = new()
        {
            sType = VkStructureType.DescriptorSetVariableDescriptorCountAllocateInfo,
            descriptorSetCount = 1,
            pDescriptorCounts = &count
        };
        allocateInfo.pNext = &variableInfo;
        VkDescriptorSet setHandle;
        result = _api.vkAllocateDescriptorSets(&allocateInfo, &setHandle);
        if (result != VkResult.Success) throw new InvalidOperationException($"Indexed descriptor set allocation failed: {result}");
        _set = setHandle;
    }

    public uint AllocateIndex()
    {
        if (_releasedIndices.Count > 0) return _releasedIndices.Pop();
        if (_nextIndex >= Capacity) throw new InvalidOperationException("Indexed texture descriptor capacity exhausted.");
        return _nextIndex++;
    }

    public void ReleaseIndex(uint index)
    {
        if (index >= _nextIndex) return;
        _releasedIndices.Push(index);
    }

    public void Write(uint index, VkImageView view, VkSampler sampler)
    {
        VkDescriptorImageInfo image = new() { imageLayout = VkImageLayout.ShaderReadOnlyOptimal, imageView = view, sampler = sampler };
        VkWriteDescriptorSet write = new()
        {
            sType = VkStructureType.WriteDescriptorSet,
            dstSet = _set,
            dstBinding = 0,
            dstArrayElement = index,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            pImageInfo = &image
        };
        _api.vkUpdateDescriptorSets(1, &write, 0, null);
    }

    public void Dispose()
    {
        if (_set.IsNotNull)
        {
            VkDescriptorSet set = _set;
            _api.vkFreeDescriptorSets(_pool, 1, &set);
        }
        if (_layout.IsNotNull) _api.vkDestroyDescriptorSetLayout(_layout);
        if (_pool.IsNotNull) _api.vkDestroyDescriptorPool(_pool);
    }
}
