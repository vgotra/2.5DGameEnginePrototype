using System.IO;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public sealed class TextureLibrary
{
    private readonly Dictionary<string, TextureHandle> _tiles = new();

    public TextureHandle Player { get; }
    public TextureHandle Deer { get; }
    public TextureHandle Rabbit { get; }

    public TextureLibrary(IRenderer renderer)
    {
        Player = PngLoader.Load(renderer, AssetPath("player")) ?? ProceduralTextures.UkraineFlag(renderer);
        Deer = PngLoader.Load(renderer, AssetPath("deer")) ?? ProceduralTextures.Blob(renderer, new(0.3f, 0.6f, 0.35f, 1f));
        Rabbit = PngLoader.Load(renderer, AssetPath("rabbit")) ?? ProceduralTextures.Blob(renderer, new(0.95f, 0.55f, 0.65f, 1f));

        foreach (string name in new[] { "grass", "water", "tree", "bonfire", "wall" })
        {
            TextureHandle? texture = PngLoader.Load(renderer, AssetPath(name));
            if (texture.HasValue) _tiles[name] = texture.Value;
        }
    }

    public TextureHandle? TryGetTile(string name) => _tiles.TryGetValue(name, out TextureHandle handle) ? handle : null;

    private static string AssetPath(string name)
        => Path.Combine(AppContext.BaseDirectory, "textures", name + ".png");
}
