using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public readonly record struct RenderSprite(
    Vector2 Position,
    Vector2 Size,
    Vector4 Color,
    TextureHandle Texture,
    float Depth)
{
    public Vector4 BottomColor { get; init; }
    public float Scale { get; init; } = 1f;
    public byte AnimationFrame { get; init; }
    public Vector2 UvScale { get; init; } = Vector2.One;
    public Vector2 UvOffset { get; init; } = Vector2.Zero;
    public BlendMode Blend { get; init; } = BlendMode.Alpha;
}

public sealed class RenderContext : IDisposable
{
    private readonly IRenderer _renderer;
    private readonly SpritePacket[] _packets;

    internal RenderContext(IRenderer renderer, int spriteCapacity)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        if (spriteCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(spriteCapacity));
        _renderer = renderer;
        _packets = new SpritePacket[spriteCapacity];
    }

    public Vector2 Viewport { get; private set; }
    public int SpriteCount { get; private set; }

    public TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter = TextureFilter.Linear)
        => _renderer.UploadTexture(rgba, width, height, filter);

    public bool ReleaseTexture(TextureHandle texture) => _renderer.ReleaseTexture(texture);

    public void BeginFrame(Vector2 viewport)
    {
        Viewport = viewport;
        SpriteCount = 0;
        _renderer.BeginFrame(viewport);
    }

    public bool Draw(in RenderSprite sprite)
    {
        if ((uint)SpriteCount >= (uint)_packets.Length) return false;
        _packets[SpriteCount++] = new SpritePacket(
            sprite.Position,
            sprite.Size,
            sprite.Color,
            sprite.Texture,
            default,
            sprite.Depth)
        {
            BottomColor = sprite.BottomColor,
            Scale = sprite.Scale,
            AnimationFrame = sprite.AnimationFrame,
            UvScale = sprite.UvScale,
            UvOffset = sprite.UvOffset,
            Blend = sprite.Blend
        };
        return true;
    }

    public void EndFrame() => _renderer.Submit(_packets.AsSpan(0, SpriteCount));

    internal void Present()
    {
        _renderer.EndFrame();
    }

    public void Resize(int width, int height) => _renderer.Resize(width, height);

    public void Dispose() => _renderer.Dispose();
}
