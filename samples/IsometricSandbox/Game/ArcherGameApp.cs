using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Platform;
using Engine.Platform.Desktop;
using Engine.Rendering;
using Engine.Rendering.Vulkan;
using Engine.Threading;
using RuntimeWorld = Engine.App.World;
using SparseWorld = Engine.Ecs.Sparse.World;

namespace IsometricSandbox.Game;

// The "Archer in the Forest" game as an ECS app: an archetype world with
// four systems (player movement, critter AI, integration, projectiles), the
// Vulkan render path, and the splash. A --simulation mode spawns a 100k
// critter herd on a procedural map to stress the parallel job system.
public sealed class ArcherGameApp : GameHost, IDisposable
{
    private const double SplashFramesPerSecond = 30;
    private const int ParallelTileThreshold = 10_000;

    private static readonly Vector4 White = new(1, 1, 1, 1);
    private static readonly Vector4 DeerColor = new(0.55f, 0.85f, 0.55f, 1);
    private static readonly Vector4 RabbitColor = new(0.95f, 0.65f, 0.75f, 1);
    private static readonly Vector2 PlayerSize = new(44, 56);

    private readonly Options _options;
    private readonly PlatformSession _session;
    private readonly JobSystem _jobs;
    private readonly VulkanRenderer _renderer;
    private readonly TileMap _map;
    private SparseWorld EcsWorld => _runtimeWorld!.EcsWorld;
    private RuntimeWorld? _runtimeWorld;
    private Scene? _scene;
    private readonly FrameScheduler _scheduler;
    private readonly WorldCommandBuffer _buffer = new();
    private readonly TextureLibrary _textures;
    private readonly BitmapFont _font;
    private readonly SplashScreen _splash;
    private readonly Random _random = new(1337);
    private readonly Random _flicker = new(7);
    private readonly PlayerMoveSystem _playerMove;
    private readonly CritterSystem _critters;
    private readonly IntegrateSystem _integrate;
    private readonly ProjectileSystem _projectiles;
    private readonly bool _simulation;
    private readonly bool _forceParallel;
    private int _simulationFrames;

    private Entity _player;
    private Vector2 _playerStart;
    private int _score;
    private int _lastScore = -1;
    private string _title = SampleConfig.WindowTitle;
    private int _lastSpriteCount;

    private SpritePacket[][]? _bands;
    private int[]? _bandCounts;
    private Random[]? _bandFlickers;
    private TileExtractionDispatch? _tileWork;

    public static ArcherGameApp Create(Options options)
    {
        PlatformSession session = GamePlatform.CreateWindow(SampleConfig.WindowTitle, SampleConfig.WindowWidth, SampleConfig.WindowHeight);
        JobSystem jobs = new();
        VulkanRenderer renderer = new(session.Window.NativeSurface, jobs);
        return new ArcherGameApp(options, session, jobs, renderer);
    }

    private ArcherGameApp(Options options, PlatformSession session, JobSystem jobs, VulkanRenderer renderer)
        : base(BuildConfig(options), session.Window, session.Input, renderer, jobs)
    {
        _options = options;
        _session = session;
        _jobs = jobs;
        _scheduler = new FrameScheduler(jobs);
        _renderer = renderer;
        _simulation = options.Simulation;
        _forceParallel = options.ForceParallel;

        _map = _simulation ? BuildSimulationMap() : new TileMap();
        if (!_simulation) _map.LoadLayout(MapLayout.Rows);
        SetGrid(_map.ToTileGrid());
        Camera.Mode = options.FlatMode ? GameMode.TopDown : GameMode.Isometric;
        _playerStart = _map.TileToWorld(MapLayout.PlayerSpawnX, MapLayout.PlayerSpawnY);

        _textures = new TextureLibrary(Renderer);
        _font = new BitmapFont(Renderer);
        _splash = new SplashScreen(_font, Viewport);

        _playerMove = new PlayerMoveSystem(_map, Input);
        _critters = new CritterSystem(_map, _simulation);
        _integrate = new IntegrateSystem(_map);
        _projectiles = new ProjectileSystem(_map) { Buffer = _buffer };
        _scheduler.Register(_playerMove);
        _scheduler.Register(_critters);
        _scheduler.Register(_integrate);
        _scheduler.Register(_projectiles);

        if (_simulation)
        {
            for (int i = 0; i < _textures.StepCount; i++) _textures.LoadNextStep();
        }
        else
        {
            _textures.BeginAsyncLoad(Jobs);
        }
    }

    protected override bool ShowSplash => !_simulation;
    protected override bool TexturesLoaded => _textures.IsComplete;
    protected override int SplashPercent
    {
        get
        {
            int steps = Math.Max(1, _textures.StepCount);
            return 5 + _textures.Progress * 95 / steps;
        }
    }

    protected override string FrameTitle()
    {
        if (_simulation) return SampleConfig.WindowTitle;
        if (_score != _lastScore)
        {
            _lastScore = _score;
            _title = $"{SampleConfig.WindowTitle} — Score {_score}";
        }
        return _title;
    }

    protected override int SpriteCount => _lastSpriteCount;

    protected override void OnSplashFrame(int percent)
    {
        _textures.TryUploadNextStep();
        int count = _splash.Render(Sprites, SampleConfig.WindowTitle, percent);
        Present(Sprites.Slice(0, count));
    }

    protected override void OnSplashComplete()
    {
        _runtimeWorld = base.CreateWorld("Sanctuary");
        _scene = _runtimeWorld.LoadScene("Forest");
        CreateWorld();
        _playerMove.Player = _player;
        _integrate.Player = _player;
        Camera.Follow(EcsWorld.Get<Position>(_player).Value, Grid!);
    }

    protected override void OnResize()
    {
        _splash.Resize(Viewport);
        _renderer.Resize((int)Viewport.X, (int)Viewport.Y);
    }

    protected override void OnPerFrame()
    {
        if (_simulation && ++_simulationFrames >= SampleConfig.SimulationFrames)
        {
            Window.Close();
            return;
        }
        if (!Input.MousePressed) return;
        ref Position playerPosition = ref EcsWorld.Get<Position>(_player);
        Camera.Follow(playerPosition.Value, Grid!);
        ref PlayerState state = ref EcsWorld.Get<PlayerState>(_player);
        state.AimTarget = Camera.ScreenToWorld(Input.MousePosition, Grid!);
        state.PendingShot = true;
    }

    protected override void OnFixedStep(float deltaSeconds)
    {
        _scheduler.Run(EcsWorld, deltaSeconds);
        _buffer.Apply(EcsWorld);
        _buffer.Clear();
        _score += _projectiles.LastKills;

        ref PlayerState state = ref EcsWorld.Get<PlayerState>(_player);
        if (state.PendingShot)
        {
            state.PendingShot = false;
            Vector2 origin = EcsWorld.Get<Position>(_player).Value;
            Vector2 direction = state.AimTarget - origin;
            if (direction.LengthSquared() >= 0.0001f && EcsWorld.Query<Position, ArrowProjectile>().Count < SampleConfig.MaxArrows)
            {
                Entity arrow = EcsWorld.Create();
                EcsWorld.AddComponent(arrow, new Position(origin));
                EcsWorld.AddComponent(arrow, new ArrowProjectile
                {
                    Direction = Vector2.Normalize(direction),
                    Speed = SampleConfig.ArrowSpeed,
                    Lifetime = SampleConfig.ArrowLifetime,
                });
            }
        }
    }

    protected override void OnRender()
    {
        TileGrid grid = Grid!;
        ref Position playerPosition = ref EcsWorld.Get<Position>(_player);
        Camera.Follow(playerPosition.Value, grid);

        int written;
        if (UseParallelExtraction())
        {
            int bandCount = EnsureBandBuffers();
            int rowsPerBand = (grid.Height + bandCount - 1) / bandCount;
            JobHandle tiles = _tileWork!.Schedule(Jobs, grid, Camera, _textures, _bands!, _bandCounts!, _bandFlickers!, bandCount, rowsPerBand);
            Renderer.BeginFrame(Viewport);
            Jobs.Wait(tiles);
            written = MergeBands(bandCount);
            written = DrawEntities(written, playerPosition.Value);
            SpriteExtraction.StableSortByKey(Sprites, written, SortKeyCounts, SortScratch);
            Renderer.Submit(Sprites.Slice(0, written));
            Renderer.EndFrame();
            _lastSpriteCount = written;
        }
        else
        {
            Renderer.BeginFrame(Viewport);
            written = SpriteExtraction.ExtractTiles(grid, Camera, _textures, _flicker, Sprites);
            written = DrawEntities(written, playerPosition.Value);
            SpriteExtraction.StableSortByKey(Sprites, written, SortKeyCounts, SortScratch);
            Renderer.Submit(Sprites.Slice(0, written));
            Renderer.EndFrame();
            _lastSpriteCount = written;
        }
    }

    protected override void OnRestart()
    {
        WorldCommandBuffer reset = new();
        ArrowCollectBody arrowBody = new() { Buffer = reset };
        EcsWorld.Query<ArrowProjectile>().ForEach(ref arrowBody);
        CritterCollectBody critterBody = new() { Buffer = reset };
        EcsWorld.Query<Critter>().ForEach(ref critterBody);
        reset.Apply(EcsWorld);

        _score = 0;
        _lastScore = -1;
        EcsWorld.Get<Position>(_player).Value = _playerStart;
        EcsWorld.Get<Velocity>(_player).Value = Vector2.Zero;
        EcsWorld.SetComponent(_player, PlayerState.At(_playerStart));
        RespawnCritters();
        Camera.Follow(_playerStart, Grid!);
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _jobs.Dispose();
        _session.Dispose();
    }

    private static GameHostConfig BuildConfig(Options options) => new(
        WindowTitle: SampleConfig.WindowTitle,
        RenderResolution: new Vector2(SampleConfig.WindowWidth, SampleConfig.WindowHeight),
        FrameCap: options.FrameCap,
        SpriteCapacity: options.Simulation ? SampleConfig.SimulationSpriteCapacity : SampleConfig.NormalSpriteCapacity,
        StartFullscreen: options.StartFullscreen,
        ShowMetrics: options.ShowMetrics,
        SplashFramesPerSecond: SplashFramesPerSecond,
        SplashMinimumSeconds: SampleConfig.SplashMinimumSeconds);

    private void CreateWorld()
    {
        _player = EcsWorld.Create();
        EcsWorld.AddComponent(_player, new Position(_playerStart));
        EcsWorld.AddComponent(_player, new Velocity(Vector2.Zero));
        EcsWorld.AddComponent(_player, new Collider(SampleConfig.PlayerRadius));
        EcsWorld.AddComponent(_player, PlayerState.At(_playerStart));
        _critters.Player = _player;
        RespawnCritters();
    }

    private void RespawnCritters()
    {
        if (_simulation)
        {
            TileGrid grid = Grid!;
            for (int i = 0; i < SampleConfig.SimulationCritters; i++)
            {
                Vector2 position = new(1f + _random.NextSingle() * (grid.Width - 3f), 1f + _random.NextSingle() * (grid.Height - 3f));
                AnimalSpecies species = i % 2 == 0 ? AnimalSpecies.Deer : AnimalSpecies.Rabbit;
                Entity entity = EcsWorld.Create();
                _scene!.Register(entity, EntityLifetime.Scene);
                EcsWorld.AddComponent(entity, new Position(position));
                EcsWorld.AddComponent(entity, CritterSystem.Create(species, position));
                EcsWorld.AddComponent(entity, new Renderable(_textures.Deer, new Vector2(10, 10), SimColor(i)));
            }
            return;
        }
        for (int i = 0; i < SampleConfig.AnimalCount; i++)
        {
            AnimalSpecies species = i % 2 == 0 ? AnimalSpecies.Deer : AnimalSpecies.Rabbit;
            Vector2 position = CritterSystem.FindSpawn(_map, _random, _playerStart);
            Entity entity = EcsWorld.Create();
            _scene!.Register(entity, EntityLifetime.Scene);
            EcsWorld.AddComponent(entity, new Position(position));
            EcsWorld.AddComponent(entity, CritterSystem.Create(species, position));
            EcsWorld.AddComponent(entity, new Health(1));
            EcsWorld.AddComponent(entity, new Renderable(
                species == AnimalSpecies.Deer ? _textures.Deer : _textures.Rabbit,
                species == AnimalSpecies.Deer ? new Vector2(36, 44) : new Vector2(28, 36),
                species == AnimalSpecies.Deer ? DeerColor : RabbitColor));
        }
    }

    private int DrawEntities(int written, Vector2 playerWorld)
    {
        ref PlayerState state = ref EcsWorld.Get<PlayerState>(_player);
        written = SpriteExtraction.WriteEntity(Grid!, Camera, Sprites, written, playerWorld, PlayerSize, _textures.Player, state.JumpHeight, White);
        EntityRenderBody entityBody = new() { Grid = Grid!, Camera = Camera, Sprites = SpriteArray, Written = written };
        EcsWorld.Query<Position, Renderable>().ForEach(ref entityBody);
        written = entityBody.Written;
        ArrowRenderBody arrowBody = new() { Grid = Grid!, Camera = Camera, Sprites = SpriteArray, Written = written };
        EcsWorld.Query<Position, ArrowProjectile>().ForEach(ref arrowBody);
        return arrowBody.Written;
    }

    private bool UseParallelExtraction()
    {
        TileGrid grid = Grid!;
        return _forceParallel || grid.Width * grid.Height >= ParallelTileThreshold;
    }

    private int EnsureBandBuffers()
    {
        TileGrid grid = Grid!;
        int bandCount = Math.Min(Jobs.WorkerCount, grid.Height);
        int rowsPerBand = (grid.Height + bandCount - 1) / bandCount;
        int capacity = rowsPerBand * grid.Width * 2;
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
            _bands![band].AsSpan(0, count).CopyTo(Sprites.Slice(written));
            written += count;
        }
        return written;
    }

    private static TileMap BuildSimulationMap()
    {
        int width = SampleConfig.SimulationWidth;
        int height = SampleConfig.SimulationHeight;
        TileMap map = new(width, height);
        for (int x = 0; x < width; x++)
        {
            map.SetTile(x, 0, TileType.Wall);
            map.SetTile(x, height - 1, TileType.Wall);
        }
        for (int y = 0; y < height; y++)
        {
            map.SetTile(0, y, TileType.Wall);
            map.SetTile(width - 1, y, TileType.Wall);
        }
        int riverY = height / 3;
        for (int x = 0; x < width; x++)
        {
            if (Math.Abs(x - width / 2) > 2) map.SetTile(x, riverY, TileType.Water);
        }
        Random random = new(42);
        for (int cluster = 0; cluster < 8; cluster++)
        {
            int cx = 4 + random.Next(width - 12);
            int cy = 4 + random.Next(height - 12);
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                    map.SetTile(cx + dx, cy + dy, TileType.Tree);
        }
        return map;
    }

    private static Vector4 SimColor(int i)
    {
        float t = (i % 8) / 7f;
        return new Vector4(1f - 0.5f * t, 0.8f - 0.2f * t, 1f - 0.55f * t, 1f);
    }
}
