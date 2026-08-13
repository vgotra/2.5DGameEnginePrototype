using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public struct Renderable(TextureHandle texture, Vector2 size, Vector4 color)
{
    public TextureHandle Texture = texture;
    public Vector2 Size = size;
    public Vector4 Color = color;
    public float Scale = 1f;
    public float Opacity = 1f;
    public byte AnimationFrame;
    public BlendMode Blend = BlendMode.Alpha;
    public MaterialHandle Material;

    public readonly RenderItem ToRenderItem(Vector2 worldPosition)
        => new(worldPosition, Size, Texture, new Vector4(Color.X, Color.Y, Color.Z, Color.W * Opacity), SortKey: 0f)
        {
            Material = Material,
            Scale = Scale,
            Opacity = Opacity,
            AnimationFrame = AnimationFrame,
            Blend = Blend,
        };
}
