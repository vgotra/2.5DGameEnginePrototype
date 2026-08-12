using System.IO;
using System.Numerics;
using Engine.App;
using Engine.Rendering;
using Engine.Threading;

namespace IsometricSandbox.Game;

// Holds the entity/tile textures used by the sample. Textures load one step
// at a time so the splash screen can show loading progress; with a JobSystem
// attached (BeginAsyncLoad) all PNG decodes run on worker threads up front
// while the main thread uploads the finished ones one step per splash frame.
public sealed class TextureLibrary(IRenderer renderer) : ITileTextureProvider
{
    private readonly Dictionary<string, TextureHandle> _tiles = new();
    private readonly string[] _tileNames = { "grass", "water", "tree", "bonfire", "wall" };
    private int _step;

    private JobSystem? _jobs;
    private JobHandle[] _decodeHandles = [];
    private DecodedTexture[] _decoded = [];

    public TextureHandle Player { get; private set; }
    public TextureHandle Deer { get; private set; }
    public TextureHandle Rabbit { get; private set; }

    // Three entity textures plus the tile textures, loaded one per step.
    public int StepCount => 3 + _tileNames.Length;
    public int Progress => Math.Min(_step, StepCount);
    public bool IsComplete => _step >= StepCount;

    // Schedules every PNG decode on the job system; uploads stay on the main
    // thread and happen in TryUploadNextStep as decodes finish.
    public void BeginAsyncLoad(JobSystem jobs)
    {
        _jobs = jobs;
        _decodeHandles = new JobHandle[StepCount];
        _decoded = new DecodedTexture[StepCount];
        for (int i = 0; i < StepCount; i++)
        {
            int step = i;
            _decodeHandles[i] = jobs.Schedule(() => DecodeInto(step));
        }
    }

    // Loads the next texture synchronously (decode + upload in one call).
    public void LoadNextStep()
    {
        int step = _step++;
        if (step >= StepCount) return;
        PngImage? image = PngLoader.Decode(AssetPath(NameForStep(step)));
        ApplyDecoded(step, ToDecoded(image));
    }

    // Advances one step once its decode is ready; returns false when the next
    // decode is still in flight so the caller can keep animating the splash.
    public bool TryUploadNextStep()
    {
        if (_jobs == null)
        {
            LoadNextStep();
            return true;
        }
        if (IsComplete) return true;
        int step = _step;
        if (!_jobs.IsComplete(_decodeHandles[step])) return false;
        ApplyDecoded(step, _decoded[step]);
        _decoded[step].Rgba = null;
        _step++;
        return true;
    }

    public TextureHandle? TryGetTile(string name) => _tiles.TryGetValue(name, out TextureHandle handle) ? handle : null;

    public TextureHandle? TryGet(string name) => TryGetTile(name);

    private void DecodeInto(int step)
    {
        PngImage? image = PngLoader.Decode(AssetPath(NameForStep(step)));
        _decoded[step] = ToDecoded(image);
    }

    private void ApplyDecoded(int step, DecodedTexture decoded)
    {
        switch (step)
        {
            case 0:
                Player = UploadOrNull(decoded) ?? ProceduralTextures.UkraineFlag(renderer);
                break;
            case 1:
                Deer = UploadOrNull(decoded) ?? ProceduralTextures.Blob(renderer, new(0.3f, 0.6f, 0.35f, 1f));
                break;
            case 2:
                Rabbit = UploadOrNull(decoded) ?? ProceduralTextures.Blob(renderer, new(0.95f, 0.55f, 0.65f, 1f));
                break;
            default:
                int index = step - 3;
                if (index >= _tileNames.Length) break;
                TextureHandle? uploaded = UploadOrNull(decoded);
                if (uploaded.HasValue) _tiles[_tileNames[index]] = uploaded.Value;
                break;
        }
    }

    private TextureHandle? UploadOrNull(in DecodedTexture decoded)
        => decoded.Rgba == null ? null : renderer.UploadTexture(decoded.Rgba, decoded.Width, decoded.Height, TextureFilter.Nearest);

    private string NameForStep(int step) => step switch
    {
        0 => "player",
        1 => "deer",
        2 => "rabbit",
        _ => _tileNames[step - 3]
    };

    private static DecodedTexture ToDecoded(PngImage? image)
        => image.HasValue
            ? new DecodedTexture { Rgba = image.Value.Data, Width = image.Value.Width, Height = image.Value.Height }
            : new DecodedTexture();

    private static string AssetPath(string name)
        => Path.Combine(AppContext.BaseDirectory, "textures", name + ".png");
}
