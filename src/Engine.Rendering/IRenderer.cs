using System.Numerics;

namespace Engine.Rendering;

public interface IRenderer : IDisposable
{
    void BeginFrame(Vector2 viewport);
    void Submit(ReadOnlySpan<SpritePacket> sprites);
    void EndFrame();
    TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter = TextureFilter.Linear);
}
