using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Rendering;
using Engine.Threading;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

internal unsafe class BatchRenderer : IDisposable
{
    private const int ParallelRangeThreshold = 512;
    private const int MinRangesPerChunk = 256;

    private readonly VkDevice _device;
    private readonly VkDeviceApi _deviceApi;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkPhysicalDeviceMemoryProperties _memoryProperties;
    private readonly VulkanPipeline _pipeline;
    private readonly DescriptorSetAllocator _descriptorAllocator;
    private readonly TextureUploader _textureUploader;
    private readonly VkQueue _graphicsQueue;
    private readonly uint _framesInFlight;
    private readonly ParallelDrawRecorder _drawRecorder;
    private readonly JobSystem _jobSystem;

    private VulkanBuffer[] _vertexBuffers;
    private VulkanBuffer[] _indexBuffers;
    private VulkanBuffer[] _stagingVertexBuffers;
    private VulkanBuffer[] _stagingIndexBuffers;
    private readonly FrameGeometryCache _geometryCache;
    private int _frameIndex;

    private readonly GrowableBuffer<ShapeVertex> _vertices = new();
    private readonly GrowableBuffer<uint> _indices = new();
    private readonly List<TextureDrawRange> _textureRanges = new();

    private FrameRenderContext _context;

    public BatchRenderer(
        VkDevice device,
        VkDeviceApi deviceApi,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        VulkanPipeline pipeline,
        DescriptorSetAllocator descriptorAllocator,
        TextureUploader textureUploader,
        VkQueue graphicsQueue,
        uint framesInFlight,
        ParallelDrawRecorder drawRecorder,
        JobSystem jobSystem)
    {
        _device = device;
        _deviceApi = deviceApi;
        _physicalDevice = physicalDevice;
        _memoryProperties = memoryProperties;
        _pipeline = pipeline;
        _descriptorAllocator = descriptorAllocator;
        _textureUploader = textureUploader;
        _graphicsQueue = graphicsQueue;
        _framesInFlight = framesInFlight;
        _drawRecorder = drawRecorder;
        _jobSystem = jobSystem;

        _vertexBuffers = new VulkanBuffer[framesInFlight];
        _indexBuffers = new VulkanBuffer[framesInFlight];
        _stagingVertexBuffers = new VulkanBuffer[framesInFlight];
        _stagingIndexBuffers = new VulkanBuffer[framesInFlight];
        _geometryCache = new FrameGeometryCache((int)framesInFlight);
    }

    public void ResizeBuffers(uint maxVertices, uint maxIndices)
    {
        _vertices.EnsureCapacity((int)maxVertices);
        _indices.EnsureCapacity((int)maxIndices);
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
        _vertices.Clear();
        _indices.Clear();
        _textureRanges.Clear();
    }

    public void Submit(ReadOnlySpan<SpritePacket> sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            var sprite = sprites[i];
            AddShape(new ShapePacket(sprite.Position, sprite.Size, sprite.Color, sprite.SortKey, sprite.Shape), sprite.Texture);
        }
    }

    private void AddShape(ShapePacket packet, TextureHandle texture)
    {
        uint firstIndex = (uint)_indices.Count;
        int vertexOffset = _vertices.Count;
        if (packet.Shape == ShapeKind.Box) AddBoxShape(packet, vertexOffset);
        else AddDiamondShape(packet, vertexOffset);
        _textureRanges.Add(new TextureDrawRange(texture, firstIndex, 6));
    }

    private void AddBoxShape(ShapePacket packet, int vertexOffset)
    {
        float halfWidth = packet.Size.X * 0.5f;
        float halfHeight = packet.Size.Y * 0.5f;

        Vector2 topLeft = packet.Position + new Vector2(-halfWidth, -halfHeight);
        Vector2 topRight = packet.Position + new Vector2(halfWidth, -halfHeight);
        Vector2 bottomRight = packet.Position + new Vector2(halfWidth, halfHeight);
        Vector2 bottomLeft = packet.Position + new Vector2(-halfWidth, halfHeight);

        _vertices.Add(new ShapeVertex(topLeft, packet.Color, new Vector2(0, 0)));
        _vertices.Add(new ShapeVertex(topRight, packet.Color, new Vector2(1, 0)));
        _vertices.Add(new ShapeVertex(bottomRight, packet.Color, new Vector2(1, 1)));
        _vertices.Add(new ShapeVertex(bottomLeft, packet.Color, new Vector2(0, 1)));
        AppendQuadIndices(vertexOffset);
    }

    private void AddDiamondShape(ShapePacket packet, int vertexOffset)
    {
        float halfWidth = packet.Size.X * 0.5f;
        float halfHeight = packet.Size.Y * 0.5f;

        Vector2 top = packet.Position + new Vector2(0, -halfHeight);
        Vector2 right = packet.Position + new Vector2(halfWidth, 0);
        Vector2 bottom = packet.Position + new Vector2(0, halfHeight);
        Vector2 left = packet.Position + new Vector2(-halfWidth, 0);

        _vertices.Add(new ShapeVertex(top, packet.Color, new Vector2(0.5f, 0)));
        _vertices.Add(new ShapeVertex(right, packet.Color, new Vector2(1, 0.5f)));
        _vertices.Add(new ShapeVertex(bottom, packet.Color, new Vector2(0.5f, 1)));
        _vertices.Add(new ShapeVertex(left, packet.Color, new Vector2(0, 0.5f)));
        AppendQuadIndices(vertexOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendQuadIndices(int vertexOffset)
    {
        _indices.Add((uint)vertexOffset + 0);
        _indices.Add((uint)vertexOffset + 1);
        _indices.Add((uint)vertexOffset + 2);
        _indices.Add((uint)vertexOffset + 0);
        _indices.Add((uint)vertexOffset + 2);
        _indices.Add((uint)vertexOffset + 3);
    }

    public void EndFrame()
    {
        FrameRenderContext context = _context;
        int chunks = 0;
        if (_vertices.Count > 0 && _indices.Count > 0)
        {
            UploadGeometry(context.Primary);
            chunks = 1;
            RecordDrawChunks(context, chunks);
        }
        BeginRenderPass(context);
        if (chunks > 0) _drawRecorder.ExecuteRecorded(context.Primary, chunks);
        _deviceApi.vkCmdEndRenderPass(context.Primary);
    }

    private int ComputeChunkCount(int rangeCount)
    {
        if (rangeCount < ParallelRangeThreshold) return 1;
        return Math.Clamp(rangeCount / MinRangesPerChunk, 1, _drawRecorder.MaxChunks);
    }

    private void RecordDrawChunks(in FrameRenderContext context, int chunks)
    {
        VulkanBuffer vertexBuffer = _vertexBuffers[_frameIndex];
        VulkanBuffer indexBuffer = _indexBuffers[_frameIndex];
        _drawRecorder.RecordChunk(0, in context, _pipeline.Pipeline, _pipeline.Layout,
            vertexBuffer.Buffer, indexBuffer.Buffer, _textureRanges, 0, _textureRanges.Count, _textureUploader);
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
        _deviceApi.vkCmdBeginRenderPass(context.Primary, &renderPassBegin, VkSubpassContents.SecondaryCommandBuffers);
    }

    private void UploadGeometry(VkCommandBuffer cmdBuffer)
    {
        var vertexBuffer = _vertexBuffers[_frameIndex];
        var indexBuffer = _indexBuffers[_frameIndex];
        var stagingVertex = _stagingVertexBuffers[_frameIndex];
        var stagingIndex = _stagingIndexBuffers[_frameIndex];

        nuint vertexBytes = (nuint)(_vertices.Count * Unsafe.SizeOf<ShapeVertex>());
        nuint indexBytes = (nuint)(_indices.Count * sizeof(uint));

        EnsureBufferCapacity(ref vertexBuffer, ref indexBuffer, ref stagingVertex, ref stagingIndex, vertexBytes, indexBytes);
        UploadGeometryIfChanged(cmdBuffer, vertexBytes, indexBytes, _vertices.AsSpan(), _indices.AsSpan(), vertexBuffer, indexBuffer, stagingVertex, stagingIndex);
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
        ResizeBuffers((uint)_vertices.Count * 2, (uint)_indices.Count * 2);
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
