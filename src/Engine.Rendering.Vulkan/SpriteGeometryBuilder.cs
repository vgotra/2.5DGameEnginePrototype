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
        Vector2 uvScale = sprite.AnimationFrame == 0 ? Vector2.One : new Vector2(1f / 8f, 1f);
        Vector2 uvOffset = sprite.AnimationFrame == 0 ? Vector2.Zero : new Vector2(sprite.AnimationFrame * uvScale.X, 0f);
        AddDiamondShape(sprite.Position, sprite.Size, sprite.Color, vertexOffset, uvScale, uvOffset);
        _textureRanges.Add(new TextureDrawRange(sprite.Texture, sprite.Material, sprite.Blend, firstIndex, 6));
    }

    private void AddDiamondShape(Vector2 position, Vector2 size, Vector4 color, int vertexOffset, Vector2 uvScale, Vector2 uvOffset)
    {
        float halfWidth = size.X * 0.5f;
        float halfHeight = size.Y * 0.5f;
        Vector2 top = position + new Vector2(0, -halfHeight);
        Vector2 right = position + new Vector2(halfWidth, 0);
        Vector2 bottom = position + new Vector2(0, halfHeight);
        Vector2 left = position + new Vector2(-halfWidth, 0);
        _vertices.Add(new ShapeVertex(top, color, uvOffset + new Vector2(0.5f, 0) * uvScale));
        _vertices.Add(new ShapeVertex(right, color, uvOffset + new Vector2(1, 0.5f) * uvScale));
        _vertices.Add(new ShapeVertex(bottom, color, uvOffset + new Vector2(0.5f, 1) * uvScale));
        _vertices.Add(new ShapeVertex(left, color, uvOffset + new Vector2(0, 0.5f) * uvScale));
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
