using System.Numerics;

namespace Engine.Rendering;

public readonly record struct SpritePacket(Vector2 Position, Vector2 Size, Vector4 Color, TextureHandle Texture, MaterialHandle Material, float SortKey)
{
    public float Scale { get; init; } = 1f;
    public byte AnimationFrame { get; init; }
    public BlendMode Blend { get; init; } = BlendMode.Alpha;
}
