using System.Numerics;
using System.Diagnostics;
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

// The "Archer in the Forest" game as an ECS app: a sparse world with
// ARPG gameplay, typed scenes, deferred ECS commands, the Vulkan render path,
// and the splash.
public sealed class ArcherGameApp : GameHost, IDisposable
{
    private const double SplashFramesPerSecond = 30;
    private const int ParallelTileThreshold = 10_000;

    private static readonly Vector4 White = new(1, 1, 1, 1);
    private static readonly Vector2 PlayerSize = new(44, SampleConfig.PlayerSpriteHeight);

    private readonly PlatformSession _session;
    private readonly JobSystem _jobs;
    private readonly VulkanRenderer _renderer;
    private readonly TerrainSurface _map;
    private SparseWorld EcsWorld => _runtimeWorld!.EcsWorld;
    private RuntimeWorld? _runtimeWorld;
    private readonly FrameScheduler _scheduler;
    private SampleEntitySpawner? _spawner;
    private readonly TextureLibrary _textures;
    private readonly SplashFont _font;
    private readonly SplashScreen _splash;
    private readonly Random _flicker = new(7);
    private readonly PlayerMoveSystem _playerMove;
    private readonly MonsterMovementSystem _monsters;
    private readonly IntegrateSystem _integrate;
    private readonly ProjectileSystem _projectiles;
    private readonly VfxPool _vfxPool;
    private readonly RenderItem[] _vfxRenderItems = new RenderItem[256];
    private static readonly SkillDefinition BasicShot = new(SkillIds.BasicShot, 0.1f, 1f);
    private static readonly WeaponDefinition Bow = new(GameContent.GoblinSlayerBow, 1f, SampleConfig.ArrowSpeed, SampleConfig.ArrowLifetime);
    private readonly VfxSystem _vfx;
    private readonly PresentationPositionHistory _presentation = new();
    private readonly LifetimeSystem _lifetimes;
    private readonly bool _forceParallel;
    private readonly double _frameCap;
    private readonly int? _frameLimit;
    private int _frameCount;

    private Entity _player;
    private Vector2 _playerStart;
    private int _score;
    private int _lastScore = -1;
    private string _title = SampleConfig.WindowTitle;
    private int _lastSpriteCount;
    private Scene? _activeScene;
    private GameProgression? _gameProgression;
    private InputActionBuffer _inputActions;
    private readonly InputBindingMap _inputBindings = new();

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
        _session = session;
        _jobs = jobs;
        _scheduler = new FrameScheduler(jobs);
        _renderer = renderer;
        _forceParallel = options.ForceParallel;
        _frameCap = options.FrameCap;
        _frameLimit = options.FrameLimit;

        _map = new TerrainSurface(20, 20);
        _map.LoadLayout(MapLayout.Rows);
        SetTerrain(_map);
        _playerStart = _map.TileToWorld(MapLayout.PlayerSpawnX, MapLayout.PlayerSpawnY);

        _textures = new TextureLibrary(Renderer);
        _font = new SplashFont(Renderer, Path.Combine(AppContext.BaseDirectory, "textures", "splash-font.png"));
        _splash = new SplashScreen(_font, Viewport);

        _playerMove = new PlayerMoveSystem(_map);
        _monsters = new MonsterMovementSystem(_map);
        _integrate = new IntegrateSystem(_map);
        _projectiles = new ProjectileSystem(_map);
        _vfxPool = new VfxPool(256);
        _vfx = new VfxSystem();
        _lifetimes = new LifetimeSystem();
        _scheduler.Register(_playerMove, new("Input.PlayerMovement", ExecutionPolicy.Serial, 0, true, true, false));
        _scheduler.Register(_monsters, new("AI.GoblinMovement", ExecutionPolicy.Serial, 0, true, true, false));
        _scheduler.Register(_integrate, new("Collision.Integration", ExecutionPolicy.Serial, 0, true, true, false));
        _scheduler.Register(_projectiles, new("Combat.Projectiles", ExecutionPolicy.Adaptive, SampleConfig.ProjectileParallelThreshold, true, true, false));
        _scheduler.Register(_vfx, new("Presentation.Vfx", ExecutionPolicy.Serial, 0, true, true, false));
        _scheduler.Register(_lifetimes, new("Presentation.Lifetimes", ExecutionPolicy.Serial, 0, true, true, false));

        _textures.BeginAsyncLoad(Jobs);
    }

    protected override bool ShowSplash => true;
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
        int count = _splash.Render(Sprites, SampleConfig.WindowTitle, SplashPercent);
        Present(Sprites.Slice(0, count));
    }

    protected override void OnSplashComplete()
    {
        _runtimeWorld = base.CreateWorld("Sanctuary");
        InitializeGameWorld();
        PresentationDiagnostics presentation = _renderer.Presentation;
        Console.WriteLine($"presentation  requested={presentation.RequestedMode}  selected={presentation.SelectedMode}  fallback={presentation.UsedFallback}  images={presentation.SwapchainImageCount}  cap={(_frameCap > 0 ? _frameCap.ToString("F0") : "unbounded")}");
        _playerMove.Player = _player;
        _integrate.Player = _player;
        Camera.Follow(EcsWorld.Get<Position>(_player).Value, Terrain!);
    }

    protected override void OnResize()
    {
        _splash.Resize(Viewport);
        _renderer.Resize((int)Viewport.X, (int)Viewport.Y);
    }

    protected override void OnPerFrame()
    {
        InputActionMapper.CaptureCurrent(Input, ref _inputActions, _inputBindings);
        _playerMove.SetCommand(_inputActions.Snapshot());
        if (_frameLimit is int limit && ++_frameCount >= limit)
        {
            Window.Close();
            return;
        }
        if (!_inputActions.Snapshot().IsPressed(InputAction.PrimaryAttack)) return;
        ref Position playerPosition = ref EcsWorld.Get<Position>(_player);
        Camera.Follow(playerPosition.Value, Terrain!);
        ref PlayerState state = ref EcsWorld.Get<PlayerState>(_player);
        state.AimTarget = Camera.ScreenToWorld(Input.MousePosition, Terrain!);
        state.PendingShot = true;
    }

    protected override void OnFixedStep(float deltaSeconds)
    {
        _gameProgression!.Tick(_inputActions.Snapshot());
        _presentation.BeginStep(EcsWorld);
        _vfxPool.Update(deltaSeconds);
        ref AbilityState abilityState = ref EcsWorld.Get<AbilityState>(_player);
        AbilityPipeline.Tick(ref abilityState, deltaSeconds);
        long schedulerStart = Stopwatch.GetTimestamp();
        _scheduler.Run(EcsWorld, deltaSeconds);
        RecordSchedulerTime((Stopwatch.GetTimestamp() - schedulerStart) * 1000.0 / Stopwatch.Frequency);
        RecordEcsTime((Stopwatch.GetTimestamp() - schedulerStart) * 1000.0 / Stopwatch.Frequency);
        _presentation.EndStep(EcsWorld);
        _runtimeWorld!.ApplyCommands();
        _score += _projectiles.LastKills;
        for (int i = 0; i < _projectiles.LastEnemyKills; i++) _gameProgression!.RecordProjectileKill();

        ref PlayerState state = ref EcsWorld.Get<PlayerState>(_player);
        if (state.PendingShot)
        {
            state.PendingShot = false;
            Vector2 origin = EcsWorld.Get<Position>(_player).Value;
            Vector2 direction = state.AimTarget - origin;
            AbilityResult ability = AbilityPipeline.TryActivate(ref abilityState, in BasicShot, in Bow, origin, direction, 0.12f, _textures.Player, new Vector2(18, 18));
            if (ability.Activated && EcsWorld.Query<Position, ArrowProjectile>().Count < SampleConfig.MaxArrows)
            {
                ProjectileDefinition projectileDefinition = ability.Projectile;
                EffectDefinition effectDefinition = ability.Effect;
                Entity projectile = _spawner!.SpawnAbilityProjectile(in projectileDefinition);
                _runtimeWorld!.Commands.Add(projectile, new ArrowProjectile { Direction = projectileDefinition.Direction, Speed = projectileDefinition.Speed, Lifetime = projectileDefinition.Lifetime });
                _vfxPool.TryAcquire(in effectDefinition, out _);
            }
        }
    }

    protected override void OnRender()
    {
        TerrainSurface grid = Terrain!;
        ref Position playerPosition = ref EcsWorld.Get<Position>(_player);
        Vector2 renderPlayer = _presentation.TryGetInterpolated(_player, Clock.InterpolationAlpha, out Vector2 interpolatedPlayer) ? interpolatedPlayer : playerPosition.Value;
        Camera.Follow(renderPlayer, grid);

        int written;
        if (UseParallelExtraction())
        {
            int bandCount = EnsureBandBuffers();
            int rowsPerBand = (grid.Height + bandCount - 1) / bandCount;
            JobHandle tiles = _tileWork!.Schedule(Jobs, grid, Camera, _textures, _bands!, _bandCounts!, _bandFlickers!, bandCount, rowsPerBand);
            Renderer.BeginFrame(Viewport);
            Jobs.Wait(tiles);
            written = MergeBands(bandCount);
            written = RenderEntities(written, renderPlayer);
            SpriteExtraction.StableSortByKey(Sprites, written, SortKeyCounts, SortScratch);
            Renderer.Submit(Sprites.Slice(0, written));
            long presentStart = Stopwatch.GetTimestamp();
            Renderer.EndFrame();
            RecordPresentTime((Stopwatch.GetTimestamp() - presentStart) * 1000.0 / Stopwatch.Frequency);
            _lastSpriteCount = written;
        }
        else
        {
            Renderer.BeginFrame(Viewport);
            written = SpriteExtraction.ExtractTiles(grid, Camera, _textures, _flicker, Sprites);
            written = RenderEntities(written, renderPlayer);
            SpriteExtraction.StableSortByKey(Sprites, written, SortKeyCounts, SortScratch);
            Renderer.Submit(Sprites.Slice(0, written));
            long presentStart = Stopwatch.GetTimestamp();
            Renderer.EndFrame();
            RecordPresentTime((Stopwatch.GetTimestamp() - presentStart) * 1000.0 / Stopwatch.Frequency);
            _lastSpriteCount = written;
        }
    }

    protected override void OnRestart()
    {
        EntityCommands reset = _runtimeWorld!.Commands;
        ArrowCollectBody arrowBody = new() { Buffer = reset };
        EcsWorld.Query<ArrowProjectile>().ForEach(ref arrowBody);
        _runtimeWorld.ApplyCommands();

        _score = 0;
        _lastScore = -1;
        EcsWorld.Get<Position>(_player).Value = _playerStart;
        EcsWorld.Get<Velocity>(_player).Value = Vector2.Zero;
        EcsWorld.SetComponent(_player, PlayerState.At(_playerStart));
        Camera.Follow(_playerStart, Terrain!);
    }

    public void Dispose()
    {
        _textures.Dispose();
        _font.Dispose();
        _renderer.Dispose();
        _jobs.Dispose();
        _session.Dispose();
    }

    private static GameHostConfig BuildConfig(Options options) => new(
        WindowTitle: SampleConfig.WindowTitle,
        RenderResolution: new Vector2(SampleConfig.WindowWidth, SampleConfig.WindowHeight),
        FrameCap: options.FrameCap,
        SpriteCapacity: SampleConfig.ArpgSpriteCapacity,
        StartFullscreen: options.StartFullscreen,
        ShowMetrics: options.ShowMetrics,
        SplashFramesPerSecond: SplashFramesPerSecond,
        SplashMinimumSeconds: SampleConfig.SplashMinimumSeconds);

    private void InitializeGameWorld()
    {
        RuntimeWorld world = _runtimeWorld!;
        GameContent.ConfigureWorld(world);
        Scene village = world.LoadScene(GameContent.VillageScene.Value);
        GameContent.ConfigureVillage(village);
        Scene forest = world.LoadScene(GameContent.GoblinForestScene.Value);
        GameContent.ConfigureGoblinForest(forest);
        world.ChangeScene(GameContent.VillageScene.Value);
        _activeScene = village;
        string manifestPath = Path.Combine(AppContext.BaseDirectory, "assets", "game-bake.json");
        _textures.RegisterManifestAtlases(manifestPath, out _);
        _spawner = new SampleEntitySpawner(world, _textures);
        _projectiles.Buffer = world.Commands;
        _lifetimes.Buffer = world.Commands;
        _spawner.RegisterHeroDefinition();
        Vector4 warriorColor = new(0.08f, 0.28f, 0.10f, 1f);
        Vector4 archerColor = new(0.16f, 0.42f, 0.18f, 1f);
        Vector4 shamanColor = new(0.10f, 0.34f, 0.12f, 1f);
        Vector4 warriorBottomColor = new(0.16f, 0.42f, 0.18f, 1f);
        Vector4 archerBottomColor = new(0.24f, 0.55f, 0.26f, 1f);
        Vector4 shamanBottomColor = new(0.20f, 0.48f, 0.22f, 1f);
        TextureHandle enemyTexture = default;
        world.Catalog.Register(GameContent.GoblinWarrior, new MonsterDefinition(MonsterType.Goblin, Vector2.Zero, 1.2f, 0.4f, 6, enemyTexture, new Vector2(36, 44), warriorColor, warriorBottomColor));
        world.Catalog.Register(GameContent.GoblinArcher, new MonsterDefinition(MonsterType.Goblin, Vector2.Zero, 1f, 0.35f, 4, enemyTexture, new Vector2(32, 40), archerColor, archerBottomColor));
        world.Catalog.Register(GameContent.GoblinShaman, new MonsterDefinition(MonsterType.GoblinShaman, Vector2.Zero, 0.8f, 0.4f, 8, enemyTexture, new Vector2(34, 44), shamanColor, shamanBottomColor));

        MapLocation start = village.Map.Resolve("player-start");
        _player = _spawner.SpawnHero(HeroIds.Rogue, start);
        world.Commands.Add(_player, new CharacterMovement { Mode = CharacterIntentKind.Stop });
        _playerStart = start.Position;
        world.ApplyCommands();
        _gameProgression = new GameProgression(world, Terrain!);
        _gameProgression.Start(_player);
    }

    private int RenderEntities(int written, Vector2 playerWorld)
    {
        ref PlayerState state = ref EcsWorld.Get<PlayerState>(_player);
        written = SpriteExtraction.WriteEntity(Terrain!, Camera, Sprites, written, playerWorld, PlayerSize, _textures.Player, state.JumpHeight, White);
        EntityRenderBody entityBody = new() { Grid = Terrain!, Camera = Camera, Sprites = SpriteArray, Written = written, ExcludedEntity = _player, History = _presentation, InterpolationAlpha = Clock.InterpolationAlpha };
        EcsWorld.Query<Position, Renderable>().ForEach(ref entityBody);
        written = entityBody.Written;
        ArrowRenderBody arrowBody = new() { Grid = Terrain!, Camera = Camera, Sprites = SpriteArray, Written = written, History = _presentation, InterpolationAlpha = Clock.InterpolationAlpha };
        EcsWorld.Query<Position, ArrowProjectile>().ForEach(ref arrowBody);
        int vfxCount = _vfxPool.Extract(_vfxRenderItems, new Vector2(0f, -SampleConfig.PlayerSpriteHeight * 0.5f));
        for (int i = 0; i < vfxCount; i++)
            arrowBody.Written = SpriteExtraction.WriteEntity(Terrain!, Camera, SpriteArray, arrowBody.Written, in _vfxRenderItems[i]);
        return arrowBody.Written;
    }

    private bool UseParallelExtraction()
    {
        TerrainSurface grid = Terrain!;
        return _forceParallel || grid.Width * grid.Height >= ParallelTileThreshold;
    }

    private int EnsureBandBuffers()
    {
        TerrainSurface grid = Terrain!;
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

}
