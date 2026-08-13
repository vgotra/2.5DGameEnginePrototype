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
    private VulkanBuffer[] _indexBuffers;
    private VulkanBuffer[] _stagingVertexBuffers;
    private VulkanBuffer[] _stagingIndexBuffers;
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
        _indexBuffers = new VulkanBuffer[framesInFlight];
        _stagingVertexBuffers = new VulkanBuffer[framesInFlight];
        _stagingIndexBuffers = new VulkanBuffer[framesInFlight];
        _geometryCache = new FrameGeometryCache((int)framesInFlight);
    }

    public void ResizeBuffers(uint maxVertices, uint maxIndices)
    {
        _geometry.EnsureCapacity((int)maxVertices, (int)maxIndices);
        for (int i = 0; i < _framesInFlight; i++)
        {
            if (_vertexBuffers[i].Buffer.IsNotNull) _vertexBuffers[i].Dispose();
            if (_indexBuffers[i].Buffer.IsNotNull) _indexBuffers[i].Dispose();
            if (_stagingVertexBuffers[i].Buffer.IsNotNull) _stagingVertexBuffers[i].Dispose();
            if (_stagingIndexBuffers[i].Buffer.IsNotNull) _stagingIndexBuffers[i].Dispose();

            nuint vertexSize = maxVertices * (nuint)Unsafe.SizeOf<ShapeVertex>();
            nuint indexSize = maxIndices * (nuint)sizeof(uint);

            _vertexBuffers[i] = VulkanBuffer.CreateVertexBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, vertexSize);
            _indexBuffers[i] = VulkanBuffer.CreateIndexBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, indexSize);
            _stagingVertexBuffers[i] = VulkanBuffer.CreateStagingBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, vertexSize);
            _stagingIndexBuffers[i] = VulkanBuffer.CreateStagingBuffer(_device, _deviceApi, _physicalDevice, _memoryProperties, indexSize);
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
        if (_geometry.VertexCount > 0 && _geometry.IndexCount > 0)
            UploadGeometry(context.Primary);
        BeginRenderPass(context);
        if (_geometry.VertexCount > 0 && _geometry.IndexCount > 0) RecordDraw(context);
        _deviceApi.vkCmdEndRenderPass(context.Primary);
    }

    private void RecordDraw(in FrameRenderContext context)
    {
        VulkanBuffer vertexBuffer = _vertexBuffers[_frameIndex];
        VulkanBuffer indexBuffer = _indexBuffers[_frameIndex];
        Vector2 viewportSize = context.Viewport;
        VkViewport viewport = new(0, 0, viewportSize.X, viewportSize.Y, 0f, 1f);
        VkRect2D scissor = new(new VkOffset2D(0, 0), new VkExtent2D((uint)viewportSize.X, (uint)viewportSize.Y));
        _deviceApi.vkCmdSetViewport(context.Primary, 0, viewport);
        _deviceApi.vkCmdSetScissor(context.Primary, 0, scissor);
        Span<VkBuffer> vertexBuffers = stackalloc VkBuffer[1] { vertexBuffer.Buffer };
        Span<ulong> offsets = stackalloc ulong[1];
        _deviceApi.vkCmdBindVertexBuffers(context.Primary, 0, vertexBuffers, offsets);
        _deviceApi.vkCmdBindIndexBuffer(context.Primary, indexBuffer.Buffer, 0, VkIndexType.Uint32);
        CameraPushConstants pushConstants = new() { Viewport = viewportSize };
        _deviceApi.vkCmdPushConstants(context.Primary, _pipeline.Layout, VkShaderStageFlags.Vertex, 0, (uint)sizeof(CameraPushConstants), &pushConstants);
        for (int i = 0; i < _geometry.TextureRanges.Count; i++)
        {
            TextureDrawRange range = _geometry.TextureRanges[i];
            _deviceApi.vkCmdBindPipeline(context.Primary, VkPipelineBindPoint.Graphics,
                range.Blend == BlendMode.Additive ? _additivePipeline.Pipeline : _pipeline.Pipeline);
            TextureHandle texture = range.Material.Value == 0 ? range.Texture : new TextureHandle(range.Material.Value);
            VkDescriptorSet descriptorSet = _textureUploader.GetDescriptorSet(texture);
            _deviceApi.vkCmdBindDescriptorSets(context.Primary, VkPipelineBindPoint.Graphics, _pipeline.Layout, 0, descriptorSet);
            _deviceApi.vkCmdDrawIndexed(context.Primary, range.IndexCount, 1, range.FirstIndex, 0, 0);
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
        var indexBuffer = _indexBuffers[_frameIndex];
        var stagingVertex = _stagingVertexBuffers[_frameIndex];
        var stagingIndex = _stagingIndexBuffers[_frameIndex];

        nuint vertexBytes = (nuint)(_geometry.VertexCount * Unsafe.SizeOf<ShapeVertex>());
        nuint indexBytes = (nuint)(_geometry.IndexCount * sizeof(uint));

        EnsureBufferCapacity(ref vertexBuffer, ref indexBuffer, ref stagingVertex, ref stagingIndex, vertexBytes, indexBytes);
        UploadGeometryIfChanged(cmdBuffer, vertexBytes, indexBytes, _geometry.Vertices, _geometry.Indices, vertexBuffer, indexBuffer, stagingVertex, stagingIndex);
    }

    private void EnsureBufferCapacity(
        ref VulkanBuffer vertexBuffer,
        ref VulkanBuffer indexBuffer,
        ref VulkanBuffer stagingVertex,
        ref VulkanBuffer stagingIndex,
        nuint vertexBytes,
        nuint indexBytes)
    {
        if (vertexBytes <= vertexBuffer.Size && indexBytes <= indexBuffer.Size) return;
        _deviceApi.vkQueueWaitIdle(_graphicsQueue);
        ResizeBuffers((uint)_geometry.VertexCount * 2, (uint)_geometry.IndexCount * 2);
        vertexBuffer = _vertexBuffers[_frameIndex];
        indexBuffer = _indexBuffers[_frameIndex];
        stagingVertex = _stagingVertexBuffers[_frameIndex];
        stagingIndex = _stagingIndexBuffers[_frameIndex];
    }

    private void UploadGeometryIfChanged(
        VkCommandBuffer cmdBuffer,
        nuint vertexBytes,
        nuint indexBytes,
        Span<ShapeVertex> vertices,
        Span<uint> indices,
        VulkanBuffer vertexBuffer,
        VulkanBuffer indexBuffer,
        VulkanBuffer stagingVertex,
        VulkanBuffer stagingIndex)
    {
        Span<byte> vertexBytesSpan = MemoryMarshal.AsBytes(vertices);
        Span<byte> indexBytesSpan = MemoryMarshal.AsBytes(indices);
        if (!_geometryCache.HasGeometryChanged(_frameIndex, vertexBytes, indexBytes, vertexBytesSpan, indexBytesSpan))
            return;

        _geometryCache.StoreGeometry(_frameIndex, vertexBytes, indexBytes, vertexBytesSpan, indexBytesSpan);

        fixed (ShapeVertex* vertexPointer = vertices)
            stagingVertex.UploadData(vertexPointer, vertexBytes);
        fixed (uint* indexPointer = indices)
            stagingIndex.UploadData(indexPointer, indexBytes);

        VkBufferCopy vertexCopy = new() { size = vertexBytes };
        _deviceApi.vkCmdCopyBuffer(cmdBuffer, stagingVertex.Buffer, vertexBuffer.Buffer, 1, &vertexCopy);
        VkBufferCopy indexCopy = new() { size = indexBytes };
        _deviceApi.vkCmdCopyBuffer(cmdBuffer, stagingIndex.Buffer, indexBuffer.Buffer, 1, &indexCopy);

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
        VkBufferMemoryBarrier indexBarrier = new()
        {
            srcAccessMask = VkAccessFlags.TransferWrite,
            dstAccessMask = VkAccessFlags.IndexRead,
            srcQueueFamilyIndex = uint.MaxValue,
            dstQueueFamilyIndex = uint.MaxValue,
            buffer = indexBuffer.Buffer,
            offset = 0,
            size = indexBytes
        };
        VkBufferMemoryBarrier* barriers = stackalloc VkBufferMemoryBarrier[2] { vertexBarrier, indexBarrier };
        _deviceApi.vkCmdPipelineBarrier(cmdBuffer,
            VkPipelineStageFlags.Transfer,
            VkPipelineStageFlags.VertexInput,
            VkDependencyFlags.None,
            0, null, 2, barriers, 0, null);
    }

    public void Dispose()
    {
        for (int i = 0; i < _framesInFlight; i++)
        {
            _vertexBuffers[i].Dispose();
            _indexBuffers[i].Dispose();
            _stagingVertexBuffers[i].Dispose();
            _stagingIndexBuffers[i].Dispose();
        }
    }
}
