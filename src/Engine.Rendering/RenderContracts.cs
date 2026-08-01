using System.Numerics;

namespace Engine.Rendering;

public readonly record struct TextureHandle(int Value);
public readonly record struct MaterialHandle(int Value);
public readonly record struct SpritePacket(Vector2 Position, Vector2 Size, Vector4 Color, TextureHandle Texture, MaterialHandle Material, float SortKey);
public readonly record struct ShapePacket(Vector2 Position, Vector2 Size, Vector4 Color, float SortKey);

public interface IRenderer : IDisposable
{
    void BeginFrame(Vector2 viewport);
    void Submit(ReadOnlySpan<SpritePacket> sprites);
    void EndFrame();
}
