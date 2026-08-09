using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public struct Renderable
{
    public TextureHandle Texture;
    public Vector2 Size;
    public Vector4 Color;

    public Renderable(TextureHandle texture, Vector2 size, Vector4 color)
    {
        Texture = texture;
        Size = size;
        Color = color;
    }
}
