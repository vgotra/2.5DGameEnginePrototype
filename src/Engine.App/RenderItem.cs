using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public readonly record struct RenderItem(
    Vector2 WorldPosition,
    Vector2 Size,
    TextureHandle Texture,
    Vector4 Color,
    float SortKey = 0f)
{
    public MaterialHandle Material { get; init; }
    public float Scale { get; init; } = 1f;
    public float Opacity { get; init; } = 1f;
    public byte AnimationFrame { get; init; }
    public BlendMode Blend { get; init; } = BlendMode.Alpha;
    public Vector2 ScreenOffset { get; init; }
    public Vector2 UvScale { get; init; } = Vector2.One;
    public Vector2 UvOffset { get; init; }
}
