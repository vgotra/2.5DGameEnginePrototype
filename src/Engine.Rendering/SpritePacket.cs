using System.Numerics;

namespace Engine.Rendering;

public readonly record struct SpritePacket(Vector2 Position, Vector2 Size, Vector4 Color, TextureHandle Texture, MaterialHandle Material, float SortKey)
{
    public Vector4 BottomColor { get; init; }
    public float Scale { get; init; } = 1f;
    public byte AnimationFrame { get; init; }
    public Vector2 UvScale { get; init; } = Vector2.One;
    public Vector2 UvOffset { get; init; } = Vector2.Zero;
    public BlendMode Blend { get; init; } = BlendMode.Alpha;
}
