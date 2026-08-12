using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public struct Renderable(TextureHandle texture, Vector2 size, Vector4 color)
{
    public TextureHandle Texture = texture;
    public Vector2 Size = size;
    public Vector4 Color = color;
}
