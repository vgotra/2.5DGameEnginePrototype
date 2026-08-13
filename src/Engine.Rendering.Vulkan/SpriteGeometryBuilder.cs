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
            AddShape(sprite);
        }
    }

    private void AddShape(SpritePacket sprite)
    {
        uint firstIndex = (uint)_indices.Count;
        int vertexOffset = _vertices.Count;
        ShapePacket packet = new(sprite.Position, sprite.Size, sprite.Color, sprite.SortKey, sprite.Shape);
        Vector2 uvScale = sprite.AnimationFrame == 0 ? Vector2.One : new Vector2(1f / 8f, 1f);
        Vector2 uvOffset = sprite.AnimationFrame == 0 ? Vector2.Zero : new Vector2(sprite.AnimationFrame * uvScale.X, 0f);
        if (packet.Shape == ShapeKind.Box) AddBoxShape(packet, vertexOffset, uvScale, uvOffset);
        else AddDiamondShape(packet, vertexOffset, uvScale, uvOffset);
        _textureRanges.Add(new TextureDrawRange(sprite.Texture, sprite.Material, sprite.Blend, firstIndex, 6));
    }

    private void AddBoxShape(ShapePacket packet, int vertexOffset, Vector2 uvScale, Vector2 uvOffset)
    {
        float halfWidth = packet.Size.X * 0.5f;
        float halfHeight = packet.Size.Y * 0.5f;
        Vector2 topLeft = packet.Position + new Vector2(-halfWidth, -halfHeight);
        Vector2 topRight = packet.Position + new Vector2(halfWidth, -halfHeight);
        Vector2 bottomRight = packet.Position + new Vector2(halfWidth, halfHeight);
        Vector2 bottomLeft = packet.Position + new Vector2(-halfWidth, halfHeight);
        _vertices.Add(new ShapeVertex(topLeft, packet.Color, uvOffset + new Vector2(0, 0) * uvScale));
        _vertices.Add(new ShapeVertex(topRight, packet.Color, uvOffset + new Vector2(1, 0) * uvScale));
        _vertices.Add(new ShapeVertex(bottomRight, packet.Color, uvOffset + new Vector2(1, 1) * uvScale));
        _vertices.Add(new ShapeVertex(bottomLeft, packet.Color, uvOffset + new Vector2(0, 1) * uvScale));
        AppendQuadIndices(vertexOffset);
    }

    private void AddDiamondShape(ShapePacket packet, int vertexOffset, Vector2 uvScale, Vector2 uvOffset)
    {
        float halfWidth = packet.Size.X * 0.5f;
        float halfHeight = packet.Size.Y * 0.5f;
        Vector2 top = packet.Position + new Vector2(0, -halfHeight);
        Vector2 right = packet.Position + new Vector2(halfWidth, 0);
        Vector2 bottom = packet.Position + new Vector2(0, halfHeight);
        Vector2 left = packet.Position + new Vector2(-halfWidth, 0);
        _vertices.Add(new ShapeVertex(top, packet.Color, uvOffset + new Vector2(0.5f, 0) * uvScale));
        _vertices.Add(new ShapeVertex(right, packet.Color, uvOffset + new Vector2(1, 0.5f) * uvScale));
        _vertices.Add(new ShapeVertex(bottom, packet.Color, uvOffset + new Vector2(0.5f, 1) * uvScale));
        _vertices.Add(new ShapeVertex(left, packet.Color, uvOffset + new Vector2(0, 0.5f) * uvScale));
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
