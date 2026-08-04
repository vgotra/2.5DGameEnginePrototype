using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Rendering;
using Vortice.Vulkan;

namespace Engine.Rendering.Vulkan;

public unsafe class BatchRenderer : IDisposable
{
    private readonly VkDevice _device;
    private readonly VkDeviceApi _deviceApi;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkPhysicalDeviceMemoryProperties _memoryProperties;
    private readonly VulkanPipeline _pipeline;
    private readonly DescriptorSetAllocator _descriptorAllocator;
    private readonly VkQueue _graphicsQueue;
    private readonly uint _framesInFlight;

    private VulkanBuffer[] _vertexBuffers;
    private VulkanBuffer[] _indexBuffers;
    private VulkanBuffer[] _stagingVertexBuffers;
    private VulkanBuffer[] _stagingIndexBuffers;
    private byte[]?[] _vertexSnapshots;
    private byte[]?[] _indexSnapshots;
    private nuint[] _vertexSnapshotSizes;
    private nuint[] _indexSnapshotSizes;
    private int _frameIndex;

    private readonly List<ShapeVertex> _vertices = new();
    private readonly List<uint> _indices = new();

    private VkViewport _viewport;
    private VkRect2D _scissor;

    public BatchRenderer(
        VkDevice device,
        VkDeviceApi deviceApi,
        VkPhysicalDevice physicalDevice,
        VkPhysicalDeviceMemoryProperties memoryProperties,
        VulkanPipeline pipeline,
        DescriptorSetAllocator descriptorAllocator,
        VkQueue graphicsQueue,
        uint framesInFlight)
    {
        _device = device;
        _deviceApi = deviceApi;
        _physicalDevice = physicalDevice;
        _memoryProperties = memoryProperties;
        _pipeline = pipeline;
        _descriptorAllocator = descriptorAllocator;
        _graphicsQueue = graphicsQueue;
        _framesInFlight = framesInFlight;

        _vertexBuffers = new VulkanBuffer[framesInFlight];
        _indexBuffers = new VulkanBuffer[framesInFlight];
        _stagingVertexBuffers = new VulkanBuffer[framesInFlight];
        _stagingIndexBuffers = new VulkanBuffer[framesInFlight];
        _vertexSnapshots = new byte[]?[framesInFlight];
        _indexSnapshots = new byte[]?[framesInFlight];
        _vertexSnapshotSizes = new nuint[framesInFlight];
        _indexSnapshotSizes = new nuint[framesInFlight];
    }

    public void ResizeBuffers(uint maxVertices, uint maxIndices)
    {
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

    /// <summary>
    /// Starts a frame on the given frame-in-flight slot. <paramref name="frameIndex"/> selects
    /// the persistent vertex/index/staging buffer set for the slot; the renderer has already
    /// waited that slot's fence, so the buffers are not in use by any in-flight submission.
    /// </summary>
    public void BeginFrame(int frameIndex, VkCommandBuffer cmdBuffer, Vector2 viewport)
    {
        _frameIndex = frameIndex;
        _vertices.Clear();
        _indices.Clear();

        _viewport = new VkViewport
        {
            x = 0,
            y = 0,
            width = viewport.X,
            height = viewport.Y,
            minDepth = 0f,
            maxDepth = 1f
        };

        _scissor = new VkRect2D
        {
            offset = new VkOffset2D { x = 0, y = 0 },
            extent = new VkExtent2D { width = (uint)viewport.X, height = (uint)viewport.Y }
        };

        _deviceApi.vkCmdSetViewport(cmdBuffer, 0, _viewport);
        _deviceApi.vkCmdSetScissor(cmdBuffer, 0, _scissor);
        _deviceApi.vkCmdBindPipeline(cmdBuffer, VkPipelineBindPoint.Graphics, _pipeline.Pipeline);
    }

    public void Submit(ReadOnlySpan<SpritePacket> sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            var sprite = sprites[i];
            AddShape(new ShapePacket(sprite.Position, sprite.Size, sprite.Color, sprite.SortKey, sprite.Shape));
        }
    }

    private void AddShape(ShapePacket packet)
    {
        int vertexOffset = _vertices.Count;
        int indexOffset = _indices.Count;

        float halfWidth = packet.Size.X * 0.5f;
        float halfHeight = packet.Size.Y * 0.5f;

        if (packet.Shape == ShapeKind.Box)
        {
            Vector2 topLeft = packet.Position + new Vector2(-halfWidth, -halfHeight);
            Vector2 topRight = packet.Position + new Vector2(halfWidth, -halfHeight);
            Vector2 bottomRight = packet.Position + new Vector2(halfWidth, halfHeight);
            Vector2 bottomLeft = packet.Position + new Vector2(-halfWidth, halfHeight);

            _vertices.Add(new ShapeVertex(topLeft, packet.Color));
            _vertices.Add(new ShapeVertex(topRight, packet.Color));
            _vertices.Add(new ShapeVertex(bottomRight, packet.Color));
            _vertices.Add(new ShapeVertex(bottomLeft, packet.Color));
        }
        else
        {
            Vector2 top = packet.Position + new Vector2(0, -halfHeight);
            Vector2 right = packet.Position + new Vector2(halfWidth, 0);
            Vector2 bottom = packet.Position + new Vector2(0, halfHeight);
            Vector2 left = packet.Position + new Vector2(-halfWidth, 0);

            _vertices.Add(new ShapeVertex(top, packet.Color));
            _vertices.Add(new ShapeVertex(right, packet.Color));
            _vertices.Add(new ShapeVertex(bottom, packet.Color));
            _vertices.Add(new ShapeVertex(left, packet.Color));
        }

        _indices.Add((uint)vertexOffset + 0);
        _indices.Add((uint)vertexOffset + 1);
        _indices.Add((uint)vertexOffset + 2);
        _indices.Add((uint)vertexOffset + 0);
        _indices.Add((uint)vertexOffset + 2);
        _indices.Add((uint)vertexOffset + 3);
    }

    public void EndFrame(VkCommandBuffer cmdBuffer)
    {
        if (_vertices.Count == 0 || _indices.Count == 0)
            return;

        var vertexBuffer = _vertexBuffers[_frameIndex];
        var indexBuffer = _indexBuffers[_frameIndex];
        var stagingVertex = _stagingVertexBuffers[_frameIndex];
        var stagingIndex = _stagingIndexBuffers[_frameIndex];

        nuint vertexBytes = (nuint)(_vertices.Count * Unsafe.SizeOf<ShapeVertex>());
        nuint indexBytes = (nuint)(_indices.Count * sizeof(uint));

        if (vertexBytes > vertexBuffer.Size || indexBytes > indexBuffer.Size)
        {
            _deviceApi.vkQueueWaitIdle(_graphicsQueue);
            ResizeBuffers((uint)_vertices.Count * 2, (uint)_indices.Count * 2);
            vertexBuffer = _vertexBuffers[_frameIndex];
            indexBuffer = _indexBuffers[_frameIndex];
            stagingVertex = _stagingVertexBuffers[_frameIndex];
            stagingIndex = _stagingIndexBuffers[_frameIndex];
        }

        Span<ShapeVertex> vertices = CollectionsMarshal.AsSpan(_vertices);
        Span<uint> indices = CollectionsMarshal.AsSpan(_indices);
        Span<byte> vertexBytesSpan = MemoryMarshal.AsBytes(vertices);
        Span<byte> indexBytesSpan = MemoryMarshal.AsBytes(indices);
        byte[]? vertexSnapshot = _vertexSnapshots[_frameIndex];
        byte[]? indexSnapshot = _indexSnapshots[_frameIndex];
        bool geometryChanged;
        if (vertexSnapshot is null || indexSnapshot is null ||
            vertexBytes != _vertexSnapshotSizes[_frameIndex] || indexBytes != _indexSnapshotSizes[_frameIndex])
        {
            geometryChanged = true;
        }
        else
        {
            geometryChanged = !vertexBytesSpan.SequenceEqual(vertexSnapshot.AsSpan(0, (int)vertexBytes)) ||
                              !indexBytesSpan.SequenceEqual(indexSnapshot.AsSpan(0, (int)indexBytes));
        }

        if (geometryChanged)
        {
            if (vertexSnapshot?.Length != (int)vertexBytes) _vertexSnapshots[_frameIndex] = new byte[(int)vertexBytes];
            if (indexSnapshot?.Length != (int)indexBytes) _indexSnapshots[_frameIndex] = new byte[(int)indexBytes];
            vertexBytesSpan.CopyTo(_vertexSnapshots[_frameIndex]!);
            indexBytesSpan.CopyTo(_indexSnapshots[_frameIndex]!);
            _vertexSnapshotSizes[_frameIndex] = vertexBytes;
            _indexSnapshotSizes[_frameIndex] = indexBytes;

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

        Span<VkBuffer> vertexBufferBind = stackalloc VkBuffer[1] { vertexBuffer.Buffer };
        Span<ulong> vertexBufferOffsets = stackalloc ulong[1];
        _deviceApi.vkCmdBindVertexBuffers(cmdBuffer, 0, vertexBufferBind, vertexBufferOffsets);
        _deviceApi.vkCmdBindIndexBuffer(cmdBuffer, indexBuffer.Buffer, 0, VkIndexType.Uint32);

        VulkanPipeline.CameraPushConstants pushConstants = new()
        {
            Viewport = new System.Numerics.Vector2(_viewport.width, _viewport.height)
        };
        _deviceApi.vkCmdPushConstants(cmdBuffer, _pipeline.Layout, VkShaderStageFlags.Vertex, 0, (uint)sizeof(VulkanPipeline.CameraPushConstants), &pushConstants);

        _deviceApi.vkCmdDrawIndexed(cmdBuffer, (uint)_indices.Count, 1, 0, 0, 0);
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
