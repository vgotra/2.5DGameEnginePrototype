using System.IO;
using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

// Holds the entity/tile textures used by the sample. Textures load one step
// at a time (LoadNextStep) so the splash screen can show loading progress;
// SceneRenderer drains these steps during boot before the game starts.
public sealed class TextureLibrary
{
    private readonly IRenderer _renderer;
    private readonly Dictionary<string, TextureHandle> _tiles = new();
    private readonly string[] _tileNames = { "grass", "water", "tree", "bonfire", "wall" };
    private int _step;

    public TextureHandle Player { get; private set; }
    public TextureHandle Deer { get; private set; }
    public TextureHandle Rabbit { get; private set; }

    // Three entity textures plus the tile textures, loaded one per step.
    public int StepCount => 3 + _tileNames.Length;
    public int Progress => Math.Min(_step, StepCount);
    public bool IsComplete => _step >= StepCount;

    public TextureLibrary(IRenderer renderer)
    {
        _renderer = renderer;
    }

    // Loads the next texture; call repeatedly until IsComplete.
    public void LoadNextStep()
    {
        switch (_step++)
        {
            case 0:
                Player = PngLoader.Load(_renderer, AssetPath("player")) ?? ProceduralTextures.UkraineFlag(_renderer);
                break;
            case 1:
                Deer = PngLoader.Load(_renderer, AssetPath("deer")) ?? ProceduralTextures.Blob(_renderer, new(0.3f, 0.6f, 0.35f, 1f));
                break;
            case 2:
                Rabbit = PngLoader.Load(_renderer, AssetPath("rabbit")) ?? ProceduralTextures.Blob(_renderer, new(0.95f, 0.55f, 0.65f, 1f));
                break;
            default:
                int index = _step - 3 - 1;
                if (index >= _tileNames.Length) break;
                string name = _tileNames[index];
                TextureHandle? texture = PngLoader.Load(_renderer, AssetPath(name));
                if (texture.HasValue) _tiles[name] = texture.Value;
                break;
        }
    }

    public TextureHandle? TryGetTile(string name) => _tiles.TryGetValue(name, out TextureHandle handle) ? handle : null;

    private static string AssetPath(string name)
        => Path.Combine(AppContext.BaseDirectory, "textures", name + ".png");
}
