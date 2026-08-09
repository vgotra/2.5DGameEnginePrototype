using System.Numerics;
using Engine.Platform;
using Engine.Rendering;
using Engine.Rendering.Vulkan;
using Engine.Threading;

namespace IsometricSandbox.Game;

// Owns the Vulkan renderer, the texture library, and the reusable sprite
// buffers used by extraction. Submitting a frame extracts the whole scene
// (tiles + animals + player + arrows), depth-sorts it, and presents. Large
// maps (or --parallel) extract tile bands on the job system while the main
// thread waits on the swapchain, then merge in band order before sorting.
public sealed class SceneRenderer : IDisposable
{
    private const int ParallelTileThreshold = 10_000;

    private readonly VulkanRenderer _renderer;
    private readonly TextureLibrary _textures;
    private readonly JobSystem? _jobs;
    private readonly bool _forceParallel;
    private readonly SpritePacket[] _sprites;
    private readonly SpritePacket[] _spriteScratch;
    private readonly int[] _sortKeyCounts;
    private readonly Random _flicker = new(7);
    private Vector2 _viewport;

    private SpritePacket[][]? _bands;
    private int[]? _bandCounts;
    private Random[]? _bandFlickers;
    private TileExtractionDispatch? _tileWork;

    public SceneRenderer(in NativeWindowSurface surface, TileMap map, Vector2 viewport, JobSystem? jobs = null, bool forceParallel = false)
    {
        _jobs = jobs;
        _forceParallel = forceParallel;
        _renderer = new VulkanRenderer(surface, jobs);
        _textures = new TextureLibrary(_renderer);
        int tileCapacity = map.Width * map.Height;
        _sprites = new SpritePacket[tileCapacity * 2 + 128];
        _spriteScratch = new SpritePacket[tileCapacity * 2 + 128];
        _sortKeyCounts = new int[tileCapacity];
        _viewport = viewport;
    }

    // The underlying renderer, used to build procedural textures (fonts, etc.).
    public IRenderer Renderer => _renderer;

    // Textures load one step at a time so the splash can show progress.
    public int TextureSteps => _textures.StepCount;
    public int TextureProgress => _textures.Progress;
    public bool TexturesLoaded => _textures.IsComplete;
    public void BeginTextureLoad()
    {
        if (_jobs != null) _textures.BeginAsyncLoad(_jobs);
    }
    public void LoadNextTexture() => _textures.TryUploadNextStep();

    // Keeps the swapchain and the viewport used for extraction in sync with
    // the window size.
    public void Resize(Vector2 viewport)
    {
        _viewport = viewport;
        _renderer.Resize((int)viewport.X, (int)viewport.Y);
    }

    // Presents a caller-built list of sprites (used by the splash screen).
    public void Present(ReadOnlySpan<SpritePacket> sprites)
    {
        _renderer.BeginFrame(_viewport);
        _renderer.Submit(sprites);
        _renderer.EndFrame();
    }

    // Extracts and presents one frame; returns the number of sprites drawn.
    public int SubmitFrame(
        TileMap map,
        IsometricCamera camera,
        ReadOnlySpan<Animal> animals,
        ReadOnlySpan<Arrow> arrows,
        Vector2 playerWorld,
        float jumpHeight)
    {
        if (!UseParallelExtraction(map))
            return SubmitFrameSerial(map, camera, animals, arrows, playerWorld, jumpHeight);

        int bandCount = EnsureBandBuffers(map);
        int rowsPerBand = (map.Height + bandCount - 1) / bandCount;
        JobHandle tiles = _tileWork!.Schedule(_jobs!, map, camera, _textures, _bands!, _bandCounts!, _bandFlickers!, bandCount, rowsPerBand);
        _renderer.BeginFrame(_viewport);
        _jobs!.Complete(tiles);
        int written = MergeBands(bandCount);
        written = RenderExtractionSystem.ExtractEntities(map, camera, _sprites, written, animals, arrows, playerWorld, jumpHeight, _textures);
        RenderExtractionSystem.StableSortByKey(_sprites, written, _sortKeyCounts, _spriteScratch);
        _renderer.Submit(_sprites.AsSpan(0, written));
        _renderer.EndFrame();
        return written;
    }

    private int SubmitFrameSerial(
        TileMap map,
        IsometricCamera camera,
        ReadOnlySpan<Animal> animals,
        ReadOnlySpan<Arrow> arrows,
        Vector2 playerWorld,
        float jumpHeight)
    {
        _renderer.BeginFrame(_viewport);
        int spriteCount = RenderExtractionSystem.ExtractScene(
            map, camera, _sprites, animals, arrows, playerWorld, jumpHeight,
            _textures, _flicker, _sortKeyCounts, _spriteScratch);
        _renderer.Submit(_sprites.AsSpan(0, spriteCount));
        _renderer.EndFrame();
        return spriteCount;
    }

    private bool UseParallelExtraction(TileMap map)
        => _jobs != null && (_forceParallel || map.Width * map.Height >= ParallelTileThreshold);

    private int EnsureBandBuffers(TileMap map)
    {
        int bandCount = Math.Min(_jobs!.WorkerCount, map.Height);
        int rowsPerBand = (map.Height + bandCount - 1) / bandCount;
        int capacity = rowsPerBand * map.Width * 2;
        if (_bands == null || _bands.Length < bandCount || _bands[0].Length < capacity)
        {
            _bands = new SpritePacket[bandCount][];
            for (int i = 0; i < bandCount; i++) _bands[i] = new SpritePacket[capacity];
            _bandCounts = new int[bandCount];
            _bandFlickers = new Random[bandCount];
            for (int i = 0; i < bandCount; i++) _bandFlickers[i] = new Random(7 + i);
            _tileWork = new TileExtractionDispatch();
        }
        return bandCount;
    }

    private int MergeBands(int bandCount)
    {
        int written = 0;
        for (int band = 0; band < bandCount; band++)
        {
            int count = _bandCounts![band];
            _bands![band].AsSpan(0, count).CopyTo(_sprites.AsSpan(written));
            written += count;
        }
        return written;
    }

    public void Dispose() => _renderer.Dispose();
}
