using System.Numerics;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal sealed unsafe class ParallelDrawRecorder : IDisposable
{
    private readonly VkDeviceApi _deviceApi;
    private readonly VkCommandPool[,] _pools;
    private readonly VkCommandBuffer[] _recorded;
    private readonly int _maxChunks;
    private readonly int _frameSlots;

    internal ParallelDrawRecorder(VkDeviceApi deviceApi, uint graphicsQueueFamily, int maxChunks, int frameSlots)
    {
        _deviceApi = deviceApi;
        _maxChunks = Math.Max(1, maxChunks);
        _frameSlots = frameSlots;
        _pools = new VkCommandPool[_maxChunks, frameSlots];
        _recorded = new VkCommandBuffer[_maxChunks];
        VkCommandPoolCreateInfo poolInfo = new()
        {
            queueFamilyIndex = graphicsQueueFamily,
            flags = VkCommandPoolCreateFlags.Transient
        };
        for (int chunk = 0; chunk < _maxChunks; chunk++)
        {
            for (int slot = 0; slot < frameSlots; slot++)
            {
                VkResult result = _deviceApi.vkCreateCommandPool(&poolInfo, out _pools[chunk, slot]);
                if (result != VkResult.Success) throw new InvalidOperationException($"Command pool creation failed: {result}");
            }
        }
    }

    internal int MaxChunks => _maxChunks;

    internal void ResetFrameSlot(int frameSlot)
    {
        for (int chunk = 0; chunk < _maxChunks; chunk++)
            _deviceApi.vkResetCommandPool(_pools[chunk, frameSlot], VkCommandPoolResetFlags.None);
    }

    internal void RecordChunk(
        int chunk,
        in FrameRenderContext context,
        VkPipeline alphaPipeline,
        VkPipeline additivePipeline,
        VkPipelineLayout layout,
        VkBuffer vertexBuffer,
        VkBuffer indexBuffer,
        IReadOnlyList<TextureDrawRange> ranges,
        int rangeStart,
        int rangeEnd,
        TextureUploader textures)
    {
        VkCommandBufferAllocateInfo allocateInfo = new()
        {
            commandPool = _pools[chunk, context.FrameSlot],
            level = VkCommandBufferLevel.Secondary,
            commandBufferCount = 1
        };
        VkCommandBuffer cmd;
        VkResult result = _deviceApi.vkAllocateCommandBuffers(&allocateInfo, &cmd);
        if (result != VkResult.Success) throw new InvalidOperationException($"Secondary command buffer allocation failed: {result}");

        VkCommandBufferInheritanceInfo inheritance = new()
        {
            renderPass = context.RenderPass,
            subpass = 0,
            framebuffer = context.Framebuffer
        };
        VkCommandBufferBeginInfo beginInfo = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit | VkCommandBufferUsageFlags.RenderPassContinue,
            pInheritanceInfo = &inheritance
        };
        result = _deviceApi.vkBeginCommandBuffer(cmd, &beginInfo);
        if (result != VkResult.Success) throw new InvalidOperationException($"Secondary command buffer begin failed: {result}");

        Vector2 viewportSize = context.Viewport;
        VkViewport viewport = new(0, 0, viewportSize.X, viewportSize.Y, 0f, 1f);
        VkRect2D scissor = new(new VkOffset2D(0, 0), new VkExtent2D((uint)viewportSize.X, (uint)viewportSize.Y));
        _deviceApi.vkCmdSetViewport(cmd, 0, viewport);
        _deviceApi.vkCmdSetScissor(cmd, 0, scissor);
        _deviceApi.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, alphaPipeline);

        Span<VkBuffer> vertexBuffers = stackalloc VkBuffer[1] { vertexBuffer };
        Span<ulong> offsets = stackalloc ulong[1];
        _deviceApi.vkCmdBindVertexBuffers(cmd, 0, vertexBuffers, offsets);
        _deviceApi.vkCmdBindIndexBuffer(cmd, indexBuffer, 0, VkIndexType.Uint32);

        CameraPushConstants pushConstants = new() { Viewport = viewportSize };
        _deviceApi.vkCmdPushConstants(cmd, layout, VkShaderStageFlags.Vertex, 0, (uint)sizeof(CameraPushConstants), &pushConstants);

        for (int i = rangeStart; i < rangeEnd; i++)
        {
            TextureDrawRange range = ranges[i];
            if (range.Blend == BlendMode.Additive)
                _deviceApi.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, additivePipeline);
            else
                _deviceApi.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, alphaPipeline);
            TextureHandle materialTexture = range.Material.Value == 0 ? range.Texture : new TextureHandle(range.Material.Value);
            VkDescriptorSet descriptorSet = textures.GetDescriptorSet(materialTexture);
            _deviceApi.vkCmdBindDescriptorSets(cmd, VkPipelineBindPoint.Graphics, layout, 0, descriptorSet);
            _deviceApi.vkCmdDrawIndexed(cmd, range.IndexCount, 1, range.FirstIndex, 0, 0);
        }

        result = _deviceApi.vkEndCommandBuffer(cmd);
        if (result != VkResult.Success) throw new InvalidOperationException($"Secondary command buffer end failed: {result}");
        _recorded[chunk] = cmd;
    }

    internal void ExecuteRecorded(VkCommandBuffer primary, int count)
    {
        fixed (VkCommandBuffer* recordedPointer = _recorded)
            _deviceApi.vkCmdExecuteCommands(primary, (uint)count, recordedPointer);
    }

    public void Dispose()
    {
        for (int chunk = 0; chunk < _maxChunks; chunk++)
        {
            for (int slot = 0; slot < _frameSlots; slot++)
            {
                if (_pools[chunk, slot].IsNotNull) _deviceApi.vkDestroyCommandPool(_pools[chunk, slot]);
            }
        }
    }
}
