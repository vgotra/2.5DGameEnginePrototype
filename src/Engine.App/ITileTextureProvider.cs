using Engine.Rendering;

namespace Engine.App;

public interface ITileTextureProvider
{
    TextureHandle? TryGet(string name);
}
