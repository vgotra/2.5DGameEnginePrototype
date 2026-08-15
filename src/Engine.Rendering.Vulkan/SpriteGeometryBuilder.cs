using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Rendering;

namespace Engine.Rendering.Vulkan;

internal sealed class SpriteGeometryBuilder
{
    private readonly GrowableBuffer<SpriteInstance> _instances = new();
    private readonly List<TextureDrawRange> _textureRanges = new();

    internal int InstanceCount => _instances.Count;
    internal IReadOnlyList<TextureDrawRange> TextureRanges => _textureRanges;
    internal Span<SpriteInstance> Instances => _instances.AsSpan();

    internal void BeginFrame()
    {
        _instances.Clear();
        _textureRanges.Clear();
    }

    internal void EnsureCapacity(int maxVertices, int maxIndices)
    {
        _instances.EnsureCapacity(maxVertices);
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
        uint firstIndex = (uint)_instances.Count;
        Vector2 uvScale = sprite.UvScale;
        Vector2 uvOffset = sprite.UvOffset;
        if (sprite.AnimationFrame != 0)
        {
            uvScale = new Vector2(1f / 8f, 1f);
            uvOffset = new Vector2(sprite.AnimationFrame * uvScale.X, 0f);
        }
        Vector4 bottomColor = sprite.BottomColor == default ? sprite.Color : sprite.BottomColor;
        _instances.Add(new SpriteInstance(sprite.Position, sprite.Size, sprite.Color, bottomColor, uvScale, uvOffset, (uint)Math.Max(0, sprite.Texture.Value)));
        if (_textureRanges.Count > 0)
        {
            int last = _textureRanges.Count - 1;
            TextureDrawRange previous = _textureRanges[last];
            if (previous.Texture == sprite.Texture && previous.Material == sprite.Material && previous.Blend == sprite.Blend &&
                previous.FirstIndex + previous.IndexCount == firstIndex)
            {
                _textureRanges[last] = previous with { IndexCount = previous.IndexCount + 1 };
                return;
            }
        }
        _textureRanges.Add(new TextureDrawRange(sprite.Texture, sprite.Material, sprite.Blend, firstIndex, 1));
    }
}
