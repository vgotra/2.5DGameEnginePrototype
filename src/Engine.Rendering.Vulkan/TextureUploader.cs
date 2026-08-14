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
    private readonly IndexedDescriptorAllocator? _indexedDescriptors;

    private readonly List<TextureResource> _textures = new();
    private readonly Queue<UploadRecord> _pendingUploads = new();
    private readonly Dictionary<int, UploadRecord> _uploadRecords = new();
    private readonly UploadBatch[] _uploadBatches = new UploadBatch[TextureUploadLimits.InFlightBatches];
    private int _nextUploadTicket;
    private long _completedUploads;
    private long _failedUploads;
    private int _descriptorHighWaterMark;
    private long _fallbackDescriptorBinds;
    private long _indexedDescriptorBinds;
    private double _maxUploadLatencyMs;

    public TextureUploadDiagnostics Diagnostics => new(
        _indexedDescriptors is null ? DescriptorMode.PerTextureSets : DescriptorMode.IndexedArray,
        _pendingUploads.Count,
        ActiveBatchCount(),
        _completedUploads,
        _failedUploads,
        PendingByteCount(),
        _textures.Count,
        TextureUploadLimits.DescriptorCapacity,
        _descriptorHighWaterMark,
        _fallbackDescriptorBinds,
        _indexedDescriptorBinds,
        _maxUploadLatencyMs);

    internal bool UsesIndexedDescriptors => _indexedDescriptors is not null;

    internal VkDescriptorSet GetPassDescriptorSet()
    {
        if (_indexedDescriptors is null) return VkDescriptorSet.Null;
        _indexedDescriptorBinds++;
        return _indexedDescriptors.Set;
    }

    internal TextureUploader(
        VkDevice device,
        VkDeviceApi deviceApi,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        VkQueue graphicsQueue,
        uint graphicsQueueFamily,
        DescriptorSetAllocator descriptorAllocator,
        IndexedDescriptorAllocator? indexedDescriptors = null)
    {
        _device = device;
        _deviceApi = deviceApi;
        _physicalDevice = physicalDevice;
        _memoryProperties = memoryProperties;
        _graphicsQueue = graphicsQueue;
        _descriptorAllocator = descriptorAllocator;
        _indexedDescriptors = indexedDescriptors;

        VkCommandPoolCreateInfo poolInfo = new() { queueFamilyIndex = graphicsQueueFamily };
        VkResult result = _deviceApi.vkCreateCommandPool(&poolInfo, out _commandPool);
        if (result != VkResult.Success) throw new InvalidOperationException($"Command pool creation failed: {result}");

        UploadTexture([byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue], 1, 1);
    }

    public TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter = TextureFilter.Linear)
    {
        TextureUploadTicket ticket = EnqueueTexture(rgba, width, height, filter);
        ProcessUploads(3);
        FlushUploads();
        if (!TryGetUploadResult(ticket, out TextureHandle handle)) throw new InvalidOperationException("Texture upload failed.");
        return handle;
    }

    public TextureUploadTicket EnqueueTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter = TextureFilter.Linear)
    {
        if (rgba.Length != width * height * 4 || _pendingUploads.Count >= TextureUploadLimits.PendingRequests)
            return TextureUploadTicket.Invalid;
        int id = _nextUploadTicket++;
        UploadRecord record = new(id, rgba.ToArray(), width, height, filter);
        _uploadRecords.Add(id, record);
        _pendingUploads.Enqueue(record);
        return new TextureUploadTicket(id);
    }

    public void ProcessUploads(int maxUploads)
    {
        PollCompletedUploads();
        int submitted = 0;
        while (submitted < maxUploads && _pendingUploads.Count > 0)
        {
            int batchIndex = FindFreeBatch();
            if (batchIndex < 0) return;
            UploadRecord record = _pendingUploads.Dequeue();
            try
            {
                BeginUpload(_uploadBatches[batchIndex] ??= new UploadBatch());
                UploadBatch batch = _uploadBatches[batchIndex];
                batch.Record = record;
                RecordUpload(batch, record);
                record.State = TextureUploadState.Submitted;
                submitted++;
            }
            catch
            {
                FailBatch(_uploadBatches[batchIndex]!, record);
            }
        }
    }

    public void PollCompletedUploads()
    {
        for (int i = 0; i < _uploadBatches.Length; i++)
        {
            UploadBatch? batch = _uploadBatches[i];
            if (batch is null || !batch.Active) continue;
            VkResult status = _deviceApi.vkGetFenceStatus(batch.Fence);
            if (status == VkResult.NotReady) continue;
            CompleteBatch(batch, status == VkResult.Success);
        }
    }

    public bool TryGetUploadResult(TextureUploadTicket ticket, out TextureHandle handle)
    {
        handle = default;
        if (!_uploadRecords.TryGetValue(ticket.Value, out UploadRecord? record) || record.State != TextureUploadState.Completed) return false;
        handle = record.Handle;
        return true;
    }

    public void FlushUploads()
    {
        while (_pendingUploads.Count > 0 || ActiveBatchCount() > 0)
        {
            ProcessUploads(3);
            for (int i = 0; i < _uploadBatches.Length; i++)
            {
                UploadBatch? batch = _uploadBatches[i];
                if (batch is null || !batch.Active) continue;
                VkFence fence = batch.Fence;
                _deviceApi.vkWaitForFences(1, &fence, true, ulong.MaxValue);
            }
            PollCompletedUploads();
        }
    }

    public VkDescriptorSet GetDescriptorSet(TextureHandle handle)
    {
        if (handle.Value < 0 || handle.Value >= _textures.Count)
            return VkDescriptorSet.Null;
        if (_indexedDescriptors is null) _fallbackDescriptorBinds++;
        return _indexedDescriptors?.Set ?? _textures[handle.Value].DescriptorSet;
    }

    public uint GetDescriptorIndex(TextureHandle handle)
        => handle.Value < 0 || handle.Value >= _textures.Count ? uint.MaxValue : _textures[handle.Value].DescriptorIndex;

    private int FindFreeBatch()
    {
        for (int i = 0; i < _uploadBatches.Length; i++) if (_uploadBatches[i] is null || !_uploadBatches[i].Active) return i;
        return -1;
    }

    private int ActiveBatchCount()
    {
        int count = 0; for (int i = 0; i < _uploadBatches.Length; i++) if (_uploadBatches[i] is not null && _uploadBatches[i].Active) count++; return count;
    }

    private long PendingByteCount()
    {
        long total = 0; foreach (UploadRecord record in _pendingUploads) total += record.Rgba.Length; return total;
    }

    private void BeginUpload(UploadBatch batch)
    {
        VkCommandBufferAllocateInfo allocateInfo = new() { commandPool = _commandPool, level = VkCommandBufferLevel.Primary, commandBufferCount = 1 };
        VkCommandBuffer commandBuffer;
        VkResult result = _deviceApi.vkAllocateCommandBuffers(&allocateInfo, &commandBuffer);
        if (result != VkResult.Success) throw new InvalidOperationException($"Upload command buffer allocation failed: {result}");
        batch.CommandBuffer = commandBuffer;
        VkFenceCreateInfo fenceInfo = new();
        result = _deviceApi.vkCreateFence(&fenceInfo, null, out VkFence fence);
        if (result != VkResult.Success) throw new InvalidOperationException($"Upload fence creation failed: {result}");
        batch.Fence = fence;
        VkCommandBufferBeginInfo beginInfo = new() { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
        result = _deviceApi.vkBeginCommandBuffer(batch.CommandBuffer, &beginInfo);
        if (result != VkResult.Success) throw new InvalidOperationException($"Upload command buffer begin failed: {result}");
        batch.Active = true;
    }

    private void RecordUpload(UploadBatch batch, UploadRecord record)
    {
        batch.Image = VulkanImage.Create(_device, _deviceApi, record.Width, record.Height, TextureFormat, VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled);
        batch.Memory = AllocateImageMemory(batch.Image);
        _deviceApi.vkBindImageMemory(batch.Image, batch.Memory, 0);
        batch.ImageView = VulkanImage.CreateView(_deviceApi, batch.Image, TextureFormat);
        batch.Sampler = CreateSampler(record.Filter);
        batch.Staging = VulkanBuffer.CreateStagingBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, (nuint)record.Rgba.Length);
        fixed (byte* data = record.Rgba) batch.Staging.UploadData(data, (nuint)record.Rgba.Length);
        VulkanImage.TransitionLayout(_deviceApi, batch.CommandBuffer, batch.Image, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal, VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer, 0, VkAccessFlags.TransferWrite);
        VkBufferImageCopy region = new() { imageSubresource = new VkImageSubresourceLayers { aspectMask = VkImageAspectFlags.Color, layerCount = 1 }, imageExtent = new VkExtent3D { width = (uint)record.Width, height = (uint)record.Height, depth = 1 } };
        _deviceApi.vkCmdCopyBufferToImage(batch.CommandBuffer, batch.Staging.Buffer, batch.Image, VkImageLayout.TransferDstOptimal, 1, &region);
        VulkanImage.TransitionLayout(_deviceApi, batch.CommandBuffer, batch.Image, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.FragmentShader, VkAccessFlags.TransferWrite, VkAccessFlags.ShaderRead);
        VkResult result = _deviceApi.vkEndCommandBuffer(batch.CommandBuffer);
        if (result != VkResult.Success) throw new InvalidOperationException($"Upload command buffer end failed: {result}");
        VkCommandBuffer commandBuffer = batch.CommandBuffer;
        VkSubmitInfo submit = new() { commandBufferCount = 1, pCommandBuffers = &commandBuffer };
        result = _deviceApi.vkQueueSubmit(_graphicsQueue, 1, &submit, batch.Fence);
        if (result != VkResult.Success) throw new InvalidOperationException($"Upload queue submit failed: {result}");
        batch.Submitted = true;
        batch.SubmittedAt = Environment.TickCount64;
        batch.Record = record;
    }

    private void CompleteBatch(UploadBatch batch, bool success)
    {
        UploadRecord record = batch.Record!;
        if (success)
        {
            VkDescriptorSet descriptorSet = VkDescriptorSet.Null;
            uint descriptorIndex = uint.MaxValue;
            if (_indexedDescriptors is not null) { descriptorIndex = _indexedDescriptors.AllocateIndex(); _indexedDescriptors.Write(descriptorIndex, batch.ImageView, batch.Sampler); }
            else { VkDescriptorSetLayout layout = _descriptorAllocator.GetLayout(VkDescriptorType.CombinedImageSampler, VkShaderStageFlags.Fragment, 0); descriptorSet = _descriptorAllocator.Allocate(layout); _descriptorAllocator.WriteTexture(descriptorSet, batch.ImageView, batch.Sampler); }
            int index = _textures.Count;
            _textures.Add(new TextureResource { Image = batch.Image, Memory = batch.Memory, ImageView = batch.ImageView, Sampler = batch.Sampler, DescriptorSet = descriptorSet, DescriptorIndex = descriptorIndex, Width = record.Width, Height = record.Height });
            record.Handle = new TextureHandle(index); record.State = TextureUploadState.Completed; _completedUploads++; _descriptorHighWaterMark = Math.Max(_descriptorHighWaterMark, _textures.Count);
        }
        else
        {
            record.State = TextureUploadState.Failed; _failedUploads++;
            if (batch.Sampler.IsNotNull) _deviceApi.vkDestroySampler(batch.Sampler);
            if (batch.ImageView.IsNotNull) _deviceApi.vkDestroyImageView(batch.ImageView);
            if (batch.Image.IsNotNull) _deviceApi.vkDestroyImage(batch.Image);
            if (batch.Memory.IsNotNull) _deviceApi.vkFreeMemory(batch.Memory);
        }
        if (batch.SubmittedAt != 0)
            _maxUploadLatencyMs = Math.Max(_maxUploadLatencyMs, Environment.TickCount64 - batch.SubmittedAt);
        batch.Staging.Dispose();
        if (batch.CommandBuffer.IsNotNull) { VkCommandBuffer commandBuffer = batch.CommandBuffer; _deviceApi.vkFreeCommandBuffers(_commandPool, 1, &commandBuffer); }
        if (batch.Fence.IsNotNull) _deviceApi.vkDestroyFence(batch.Fence);
        batch.Reset(); record.Rgba = [];
    }

    private void FailBatch(UploadBatch batch, UploadRecord record)
    {
        if (batch.Submitted && batch.Fence.IsNotNull)
        {
            VkFence fence = batch.Fence;
            _deviceApi.vkWaitForFences(1, &fence, true, ulong.MaxValue);
        }
        if (batch.Sampler.IsNotNull) _deviceApi.vkDestroySampler(batch.Sampler);
        if (batch.ImageView.IsNotNull) _deviceApi.vkDestroyImageView(batch.ImageView);
        if (batch.Image.IsNotNull) _deviceApi.vkDestroyImage(batch.Image);
        if (batch.Memory.IsNotNull) _deviceApi.vkFreeMemory(batch.Memory);
        if (batch.Staging.Buffer.IsNotNull) batch.Staging.Dispose();
        if (batch.CommandBuffer.IsNotNull) { VkCommandBuffer commandBuffer = batch.CommandBuffer; _deviceApi.vkFreeCommandBuffers(_commandPool, 1, &commandBuffer); }
        if (batch.Fence.IsNotNull) _deviceApi.vkDestroyFence(batch.Fence);
        batch.Reset();
        record.State = TextureUploadState.Failed;
        record.Rgba = [];
        _failedUploads++;
    }

    public void Dispose()
    {
        FlushUploads();
        for (int i = _textures.Count - 1; i >= 0; i--)
        {
            var tex = _textures[i];
            if (tex.Sampler.IsNotNull) _deviceApi.vkDestroySampler(tex.Sampler);
            if (tex.ImageView.IsNotNull) _deviceApi.vkDestroyImageView(tex.ImageView);
            if (tex.Image.IsNotNull) _deviceApi.vkDestroyImage(tex.Image);
            if (tex.Memory.IsNotNull) _deviceApi.vkFreeMemory(tex.Memory);
            if (_indexedDescriptors is null) _descriptorAllocator.Free(tex.DescriptorSet);
        }
        _textures.Clear();
        if (_commandPool.IsNotNull) _deviceApi.vkDestroyCommandPool(_commandPool);
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

    private struct TextureResource
    {
        public VkImage Image;
        public VkDeviceMemory Memory;
        public VkImageView ImageView;
        public VkSampler Sampler;
        public VkDescriptorSet DescriptorSet;
        public uint DescriptorIndex;
        public int Width;
        public int Height;
    }

    private sealed class UploadRecord
    {
        public UploadRecord(int id, byte[] rgba, int width, int height, TextureFilter filter) { Ticket = id; Rgba = rgba; Width = width; Height = height; Filter = filter; State = TextureUploadState.Queued; }
        public readonly int Ticket; public byte[] Rgba; public readonly int Width; public readonly int Height; public readonly TextureFilter Filter; public TextureUploadState State; public TextureHandle Handle;
    }

    private sealed class UploadBatch
    {
        public bool Active; public bool Submitted; public long SubmittedAt; public VkCommandBuffer CommandBuffer; public VkFence Fence; public VulkanBuffer Staging; public VkImage Image; public VkDeviceMemory Memory; public VkImageView ImageView; public VkSampler Sampler; public UploadRecord? Record;
        public void Reset() { Active = false; Submitted = false; SubmittedAt = 0; CommandBuffer = VkCommandBuffer.Null; Fence = VkFence.Null; Staging = default; Image = VkImage.Null; Memory = VkDeviceMemory.Null; ImageView = VkImageView.Null; Sampler = VkSampler.Null; Record = null; }
    }
}
