using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using Engine.Rendering;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal unsafe class BatchRenderer : IDisposable
{
    private readonly VkDevice _device;
    private readonly VkDeviceApi _deviceApi;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkPhysicalDeviceMemoryProperties _memoryProperties;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanPipeline _additivePipeline;
    private readonly TextureUploader _textureUploader;
    private readonly VkQueue _graphicsQueue;
    private readonly uint _framesInFlight;

    private VulkanBuffer[] _vertexBuffers;
    private VulkanBuffer[] _stagingVertexBuffers;
    private readonly FrameGeometryCache _geometryCache;
    private int _frameIndex;

    private readonly SpriteGeometryBuilder _geometry = new();

    private FrameRenderContext _context;

    public BatchRenderer(
        VkDevice device,
        VkDeviceApi deviceApi,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        VulkanPipeline pipeline,
        VulkanPipeline additivePipeline,
        TextureUploader textureUploader,
        VkQueue graphicsQueue,
        uint framesInFlight)
    {
        _device = device;
        _deviceApi = deviceApi;
        _physicalDevice = physicalDevice;
        _memoryProperties = memoryProperties;
        _pipeline = pipeline;
        _additivePipeline = additivePipeline;
        _textureUploader = textureUploader;
        _graphicsQueue = graphicsQueue;
        _framesInFlight = framesInFlight;

        _vertexBuffers = new VulkanBuffer[framesInFlight];
        _stagingVertexBuffers = new VulkanBuffer[framesInFlight];
        _geometryCache = new FrameGeometryCache((int)framesInFlight);
    }

    public void ResizeBuffers(uint maxVertices, uint maxIndices)
    {
        _geometry.EnsureCapacity((int)maxVertices, (int)maxIndices);
        for (int i = 0; i < _framesInFlight; i++)
        {
            if (_vertexBuffers[i].Buffer.IsNotNull) _vertexBuffers[i].Dispose();
            if (_stagingVertexBuffers[i].Buffer.IsNotNull) _stagingVertexBuffers[i].Dispose();

            nuint vertexSize = maxVertices * (nuint)Unsafe.SizeOf<SpriteInstance>();

            _vertexBuffers[i] = VulkanBuffer.CreateVertexBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, vertexSize);
            _stagingVertexBuffers[i] = VulkanBuffer.CreateStagingBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, vertexSize);
        }
    }

    public void BeginFrame(in FrameRenderContext context)
    {
        _context = context;
        _frameIndex = context.FrameSlot;
        _geometry.BeginFrame();
    }

    public void Submit(ReadOnlySpan<SpritePacket> sprites)
    {
        _geometry.AddSprites(sprites);
    }

    public void EndFrame()
    {
        FrameRenderContext context = _context;
        if (_geometry.InstanceCount > 0)
            UploadGeometry(context.Primary);
        BeginRenderPass(context);
        if (_geometry.InstanceCount > 0) RecordDraw(context);
        _deviceApi.vkCmdEndRenderPass(context.Primary);
    }

    private void RecordDraw(in FrameRenderContext context)
    {
        VulkanBuffer vertexBuffer = _vertexBuffers[_frameIndex];
        Vector2 viewportSize = context.Viewport;
        VkViewport viewport = new(0, 0, viewportSize.X, viewportSize.Y, 0f, 1f);
        VkRect2D scissor = new(new VkOffset2D(0, 0), new VkExtent2D((uint)viewportSize.X, (uint)viewportSize.Y));
        _deviceApi.vkCmdSetViewport(context.Primary, 0, viewport);
        _deviceApi.vkCmdSetScissor(context.Primary, 0, scissor);
        Span<VkBuffer> vertexBuffers = stackalloc VkBuffer[1] { vertexBuffer.Buffer };
        Span<ulong> offsets = stackalloc ulong[1];
        _deviceApi.vkCmdBindVertexBuffers(context.Primary, 1, vertexBuffers, offsets);
        CameraPushConstants pushConstants = new() { Viewport = viewportSize };
        _deviceApi.vkCmdPushConstants(context.Primary, _pipeline.Layout, VkShaderStageFlags.Vertex, 0, (uint)sizeof(CameraPushConstants), &pushConstants);
        if (_textureUploader.UsesIndexedDescriptors)
        {
            VkDescriptorSet descriptorSet = _textureUploader.GetPassDescriptorSet();
            _deviceApi.vkCmdBindDescriptorSets(context.Primary, VkPipelineBindPoint.Graphics, _pipeline.Layout, 0, descriptorSet);
        }
        for (int i = 0; i < _geometry.TextureRanges.Count; i++)
        {
            TextureDrawRange range = _geometry.TextureRanges[i];
            _deviceApi.vkCmdBindPipeline(context.Primary, VkPipelineBindPoint.Graphics,
                range.Blend == BlendMode.Additive ? _additivePipeline.Pipeline : _pipeline.Pipeline);
            TextureHandle texture = range.Material.Value == 0 ? range.Texture : new TextureHandle(range.Material.Value);
            if (!_textureUploader.UsesIndexedDescriptors)
            {
                VkDescriptorSet descriptorSet = _textureUploader.GetDescriptorSet(texture);
                _deviceApi.vkCmdBindDescriptorSets(context.Primary, VkPipelineBindPoint.Graphics, _pipeline.Layout, 0, descriptorSet);
            }
            _deviceApi.vkCmdDraw(context.Primary, 6, range.IndexCount, 0, range.FirstIndex);
        }
    }

    private void BeginRenderPass(in FrameRenderContext context)
    {
        VkClearValue clear = new(0.04f, 0.07f, 0.12f, 1f);
        VkRenderPassBeginInfo renderPassBegin = new()
        {
            renderPass = context.RenderPass,
            framebuffer = context.Framebuffer,
            renderArea = new VkRect2D(0, 0, context.Extent.width, context.Extent.height),
            clearValueCount = 1,
            pClearValues = &clear
        };
        _deviceApi.vkCmdBeginRenderPass(context.Primary, &renderPassBegin, VkSubpassContents.Inline);
    }

    private void UploadGeometry(VkCommandBuffer cmdBuffer)
    {
        var vertexBuffer = _vertexBuffers[_frameIndex];
        var stagingVertex = _stagingVertexBuffers[_frameIndex];

        nuint vertexBytes = (nuint)(_geometry.InstanceCount * Unsafe.SizeOf<SpriteInstance>());

        EnsureBufferCapacity(ref vertexBuffer, ref stagingVertex, vertexBytes);
        UploadGeometryIfChanged(cmdBuffer, vertexBytes, _geometry.Instances, vertexBuffer, stagingVertex);
    }

    private void EnsureBufferCapacity(
        ref VulkanBuffer vertexBuffer,
        ref VulkanBuffer stagingVertex,
        nuint vertexBytes)
    {
        if (vertexBytes <= vertexBuffer.Size) return;
        _deviceApi.vkQueueWaitIdle(_graphicsQueue);
        ResizeBuffers((uint)_geometry.InstanceCount * 2, 0);
        vertexBuffer = _vertexBuffers[_frameIndex];
        stagingVertex = _stagingVertexBuffers[_frameIndex];
    }

    private void UploadGeometryIfChanged(
        VkCommandBuffer cmdBuffer,
        nuint vertexBytes,
        Span<SpriteInstance> instances,
        VulkanBuffer vertexBuffer,
        VulkanBuffer stagingVertex)
    {
        Span<byte> vertexBytesSpan = MemoryMarshal.AsBytes(instances);
        if (!_geometryCache.HasGeometryChanged(_frameIndex, vertexBytes, 0, vertexBytesSpan, ReadOnlySpan<byte>.Empty))
            return;

        _geometryCache.StoreGeometry(_frameIndex, vertexBytes, 0, vertexBytesSpan, ReadOnlySpan<byte>.Empty);

        fixed (SpriteInstance* vertexPointer = instances)
            stagingVertex.UploadData(vertexPointer, vertexBytes);

        VkBufferCopy vertexCopy = new() { size = vertexBytes };
        _deviceApi.vkCmdCopyBuffer(cmdBuffer, stagingVertex.Buffer, vertexBuffer.Buffer, 1, &vertexCopy);

        VkBufferMemoryBarrier vertexBarrier = new()
        {
            srcAccessMask = VkAccessFlags.TransferWrite,
            dstAccessMask = VkAccessFlags.VertexAttributeRead,
            srcQueueFamilyIndex = uint.MaxValue,
            dstQueueFamilyIndex = uint.MaxValue,
            buffer = vertexBuffer.Buffer,
            offset = 0,
            size = vertexBytes
        };
        VkBufferMemoryBarrier* barriers = stackalloc VkBufferMemoryBarrier[1] { vertexBarrier };
        _deviceApi.vkCmdPipelineBarrier(cmdBuffer,
            VkPipelineStageFlags.Transfer,
            VkPipelineStageFlags.VertexShader,
            VkDependencyFlags.None,
            0, null, 1, barriers, 0, null);
    }

    public void Dispose()
    {
        for (int i = 0; i < _framesInFlight; i++)
        {
            _vertexBuffers[i].Dispose();
            _stagingVertexBuffers[i].Dispose();
        }
    }
}
