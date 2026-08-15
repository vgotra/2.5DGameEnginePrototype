using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Rendering.Vulkan;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct SpriteInstance(Vector2 position, Vector2 size, Vector4 color, Vector4 bottomColor, Vector2 uvScale, Vector2 uvOffset, uint textureIndex)
{
    public readonly Vector2 Position = position;
    public readonly Vector2 Size = size;
    public readonly Vector4 Color = color;
    public readonly Vector4 BottomColor = bottomColor;
    public readonly Vector2 UvScale = uvScale;
    public readonly Vector2 UvOffset = uvOffset;
    public readonly uint TextureIndex = textureIndex;
}
