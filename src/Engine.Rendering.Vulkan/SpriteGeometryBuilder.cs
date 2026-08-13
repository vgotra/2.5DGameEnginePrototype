using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Rendering;

namespace Engine.Rendering.Vulkan;

internal sealed class SpriteGeometryBuilder
{
    private readonly GrowableBuffer<ShapeVertex> _vertices = new();
    private readonly GrowableBuffer<uint> _indices = new();
    private readonly List<TextureDrawRange> _textureRanges = new();

    internal int VertexCount => _vertices.Count;
    internal int IndexCount => _indices.Count;
    internal IReadOnlyList<TextureDrawRange> TextureRanges => _textureRanges;
    internal Span<ShapeVertex> Vertices => _vertices.AsSpan();
    internal Span<uint> Indices => _indices.AsSpan();

    internal void BeginFrame()
    {
        _vertices.Clear();
        _indices.Clear();
        _textureRanges.Clear();
    }

    internal void EnsureCapacity(int maxVertices, int maxIndices)
    {
        _vertices.EnsureCapacity(maxVertices);
        _indices.EnsureCapacity(maxIndices);
    }

    internal void AddSprites(ReadOnlySpan<SpritePacket> sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            SpritePacket sprite = sprites[i];
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
}
