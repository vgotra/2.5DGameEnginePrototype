using System.Numerics;

namespace Engine.Rendering;

public readonly record struct TextureHandle(int Value);
public readonly record struct MaterialHandle(int Value);
public enum ShapeKind : byte { Diamond, Box }
public readonly record struct SpritePacket(Vector2 Position, Vector2 Size, Vector4 Color, TextureHandle Texture, MaterialHandle Material, float SortKey, ShapeKind Shape = ShapeKind.Diamond);
public readonly record struct ShapePacket(Vector2 Position, Vector2 Size, Vector4 Color, float SortKey, ShapeKind Shape = ShapeKind.Diamond);

public interface IRenderer : IDisposable
{
    void BeginFrame(Vector2 viewport);
    void Submit(ReadOnlySpan<SpritePacket> sprites);
    void EndFrame();
}
