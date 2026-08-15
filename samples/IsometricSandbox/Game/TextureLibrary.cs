using System.IO;
using System.Numerics;
using System.Diagnostics;
using Engine.App;
using Engine.Assets;
using Engine.Rendering;
using Engine.Threading;

namespace IsometricSandbox.Game;

// Holds the entity/tile textures used by the sample and tracks terminal asset progress.
public sealed class TextureLibrary(IRenderer renderer) : ITileTextureProvider, IDisposable
{
    private readonly Dictionary<string, TextureHandle> _tiles = new();
    private readonly string[] _tileNames = { "grass", "water", "tree", "bonfire", "wall" };
    private int _terminalCount;
    private readonly long[] _startedAt = new long[8];
    private readonly bool[] _terminal = new bool[8];
    private const long DecodeTimeoutMs = 15_000;

    private TextureAssetCatalog? _assets;
    private TextureAssetHandle[] _assetHandles = [];
    private GltfBakeEntry[] _gltfEntries = [];
    private readonly Dictionary<string, GltfCookedCharacter> _cookedCharacters = new(StringComparer.Ordinal);

    public TextureHandle Player { get; private set; }
    public TextureHandle Deer { get; private set; }
    public TextureHandle Rabbit { get; private set; }

    // Three entity textures plus the tile textures, loaded one per step.
    public int StepCount => 3 + _tileNames.Length;
    public int Progress => Math.Min(_terminalCount, StepCount);
    public bool IsComplete => _terminalCount >= StepCount;

    // thread and happen in TryUploadNextStep as decodes finish.
    public void BeginAsyncLoad(JobSystem jobs)
    {
        _assets = new TextureAssetCatalog(jobs);
        _assetHandles = new TextureAssetHandle[StepCount];
        for (int i = 0; i < StepCount; i++)
        {
            _assetHandles[i] = _assets.Request(AssetPath(NameForStep(i)), TextureFilter.Nearest);
            _startedAt[i] = Environment.TickCount64;
        }
    }

    public void LoadNextStep()
    {
        int step = _terminalCount;
        if (step >= StepCount) return;
        PngImage? image = PngLoader.Decode(AssetPath(NameForStep(step)));
        if (!image.HasValue)
        {
            ApplyFallback(step);
            MarkTerminal(step);
            return;
        }
        TextureHandle texture = renderer.UploadTexture(image.Value.Data, image.Value.Width, image.Value.Height, TextureFilter.Nearest);
        switch (step)
        {
            case 0: Player = texture; break;
            case 1: Deer = texture; break;
            case 2: Rabbit = texture; break;
            default:
                int index = step - 3;
                if (index < _tileNames.Length) _tiles[_tileNames[index]] = texture;
                break;
        }
        MarkTerminal(step);
    }

    // Advances one step once its decode is ready; returns false when the next
    // decode is still in flight so the caller can keep animating the splash.
    public bool TryUploadNextStep()
    {
        if (_assets == null)
        {
            LoadNextStep();
            return true;
        }
        if (IsComplete) return true;
        long now = Environment.TickCount64;
        for (int step = 0; step < StepCount; step++)
        {
            if (_terminal[step]) continue;
            if (_assets.TryTakeDecoded(_assetHandles[step], out DecodedTextureData decoded))
            {
                ApplyDecoded(step, in decoded);
                decoded.Dispose();
                MarkTerminal(step);
                return true;
            }
            TextureAssetState state = _assets.GetState(_assetHandles[step]);
            if (state == TextureAssetState.Failed || now - _startedAt[step] >= DecodeTimeoutMs)
            {
                if (state != TextureAssetState.Failed) _assets.MarkFailed(_assetHandles[step]);
                ApplyFallback(step);
                MarkTerminal(step);
                return true;
            }
        }
        return false;
    }

    public TextureHandle? TryGetTile(string name) => _tiles.TryGetValue(name, out TextureHandle handle) ? handle : null;

    public TextureHandle? TryGet(string name) => TryGetTile(name);

    public bool LoadGltfManifest(string path, out string error)
    {
        if (!GltfBakeManifestReader.TryRead(path, out GltfBakeEntry[] entries, out error)) return false;
        _gltfEntries = entries;
        return true;
    }

    public int RegisterManifestAtlases(string manifestPath, out string error)
    {
        error = string.Empty;
        if (!LoadGltfManifest(manifestPath, out error)) return 0;
        int registered = 0;
        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? string.Empty;
        for (int i = 0; i < _gltfEntries.Length; i++)
        {
            GltfBakeEntry entry = _gltfEntries[i];
            string atlasPath = Path.Combine(baseDirectory, entry.Atlas);
            PngImage? image = PngLoader.Decode(atlasPath);
            if (!image.HasValue) continue;
            GltfSpriteFrame[] frames = new GltfSpriteFrame[Math.Max(1, entry.Directions * Math.Max(1, entry.Clips.Length == 0 ? 1 : entry.Clips[0].FrameCount / Math.Max(1, entry.Directions)))];
            int frameCount = frames.Length;
            for (int frame = 0; frame < frameCount; frame++)
            {
                int direction = frame % entry.Directions;
                int row = frame / entry.Directions;
                frames[frame] = new GltfSpriteFrame(frame, direction, new Vector2((float)(direction * entry.FrameWidth) / entry.AtlasWidth, (float)(row * entry.FrameHeight) / entry.AtlasHeight), new Vector2((float)entry.FrameWidth / entry.AtlasWidth, (float)entry.FrameHeight / entry.AtlasHeight), Vector2.Zero);
            }
            GltfSpriteAtlas atlas = new(entry.Id, image.Value.Data, image.Value.Width, image.Value.Height, entry.FrameWidth, entry.FrameHeight, frameCount, entry.Directions) { Clip = entry.Clips.Length == 0 ? "default" : entry.Clips[0].Name, FramesPerSecond = entry.Clips.Length == 0 ? 0 : entry.Clips[0].FramesPerSecond, Frames = frames };
            UploadGltfAtlas(in atlas);
            registered++;
        }
        return registered;
    }

    public bool TryGetGltfEntry(string id, out GltfBakeEntry entry)
    {
        for (int i = 0; i < _gltfEntries.Length; i++)
            if (string.Equals(_gltfEntries[i].Id, id, StringComparison.Ordinal)) { entry = _gltfEntries[i]; return true; }
        entry = default;
        return false;
    }

    public TextureHandle UploadGltfAtlas(in GltfSpriteAtlas atlas)
    {
        TextureHandle handle = renderer.UploadTexture(atlas.Rgba, atlas.Width, atlas.Height, TextureFilter.Nearest);
        GltfBakeClip[] clips = [];
        for (int i = 0; i < _gltfEntries.Length; i++)
            if (string.Equals(_gltfEntries[i].Id, atlas.StableId, StringComparison.Ordinal)) { clips = _gltfEntries[i].Clips; break; }
        SpriteAnimationClip[] runtimeClips = new SpriteAnimationClip[clips.Length];
        for (int i = 0; i < clips.Length; i++) runtimeClips[i] = new SpriteAnimationClip(clips[i].Name, clips[i].FirstFrame, clips[i].FrameCount, clips[i].FramesPerSecond);
        Vector2[] offsets = new Vector2[atlas.Frames.Length];
        Vector2[] scales = new Vector2[atlas.Frames.Length];
        for (int i = 0; i < atlas.Frames.Length; i++) { offsets[i] = atlas.Frames[i].UvOffset; scales[i] = atlas.Frames[i].UvScale; }
        _cookedCharacters[atlas.StableId] = new GltfCookedCharacter(new GltfAssetId(atlas.StableId), handle, atlas.FrameWidth, atlas.FrameHeight, atlas.FrameCount, atlas.Directions, runtimeClips, offsets, scales);
        return handle;
    }

    public bool TryGetCookedCharacter(string id, out GltfCookedCharacter character)
        => _cookedCharacters.TryGetValue(id, out character);

    private void ApplyDecoded(int step, in DecodedTextureData decoded)
    {
        switch (step)
        {
            case 0:
                Player = renderer.UploadTexture(decoded.AsSpan(), decoded.Width, decoded.Height, decoded.Filter);
                break;
            case 1:
                Deer = renderer.UploadTexture(decoded.AsSpan(), decoded.Width, decoded.Height, decoded.Filter);
                break;
            case 2:
                Rabbit = renderer.UploadTexture(decoded.AsSpan(), decoded.Width, decoded.Height, decoded.Filter);
                break;
            default:
                int index = step - 3;
                if (index >= _tileNames.Length) break;
                _tiles[_tileNames[index]] = renderer.UploadTexture(decoded.AsSpan(), decoded.Width, decoded.Height, decoded.Filter);
                break;
        }
    }

    private void ApplyFallback(int step)
    {
        switch (step)
        {
            case 0: Player = ProceduralTextures.UkraineFlag(renderer); break;
            case 1: Deer = ProceduralTextures.Blob(renderer, new(0.3f, 0.6f, 0.35f, 1f)); break;
            case 2: Rabbit = ProceduralTextures.Blob(renderer, new(0.95f, 0.55f, 0.65f, 1f)); break;
        }
    }

    private void MarkTerminal(int step)
    {
        if (_terminal[step]) return;
        _terminal[step] = true;
        _terminalCount++;
    }

    private string NameForStep(int step) => step switch
    {
        0 => "player",
        1 => "deer",
        2 => "rabbit",
        _ => _tileNames[step - 3]
    };

    private static string AssetPath(string name)
        => Path.Combine(AppContext.BaseDirectory, "textures", name + ".png");

    public void Dispose() => _assets?.Dispose();
}
