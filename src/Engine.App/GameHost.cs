using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Rendering;
using Engine.Threading;

namespace Engine.App;

public abstract class GameHost : Game
{
    private readonly GameHostConfig _config;
    private readonly IGameWindow _window;
    private readonly IInputState _input;
    private readonly IRenderer _renderer;
    private readonly JobSystem _jobs;
    private readonly SpritePacket[] _sprites;
    private readonly SpritePacket[] _sortScratch;
    private FrameTimer _frameTimer;
    private GameClock _clock;
    private FrameMetrics _metrics;
    private int[] _sortKeyCounts = Array.Empty<int>();
    private Vector2 _viewport;
    private long _frameStartAlloc;
    private long _frameStartGen0;
    private long _frameStartGen1;
    private long _frameStartGen2;
    private string? _lastTitle;

    protected GameHost(GameHostConfig config, IGameWindow window, IInputState input, IRenderer renderer, JobSystem jobs)
    {
        _config = config;
        _window = window;
        _input = input;
        _renderer = renderer;
        _jobs = jobs;
        _sprites = new SpritePacket[config.SpriteCapacity];
        _sortScratch = new SpritePacket[config.SpriteCapacity];
        _frameTimer = new FrameTimer(config.FrameCap);
        _clock = new GameClock();
        _viewport = window.Size;
        Camera = new IsometricCamera(_viewport);
        if (config.StartFullscreen) _window.SetFullscreen(true);
    }

    public IGameWindow Window => _window;
    public IInputState Input => _input;
    public IRenderer Renderer => _renderer;
    public JobSystem Jobs => _jobs;
    public IsometricCamera Camera { get; }
    public GameClock Clock => _clock;
    public Vector2 Viewport => _viewport;
    public TileGrid? Grid { get; private set; }
    public Span<SpritePacket> Sprites => _sprites;
    public Span<SpritePacket> SortScratch => _sortScratch;
    public Span<int> SortKeyCounts => _sortKeyCounts;

    protected SpritePacket[] SpriteArray => _sprites;

    protected void SetGrid(TileGrid grid)
    {
        Grid = grid;
        _sortKeyCounts = new int[grid.Width * grid.Height];
    }

    protected virtual bool ShowSplash => true;
    protected virtual bool TexturesLoaded => true;
    protected virtual int SplashPercent => 0;
    protected virtual void OnSplashFrame(int percent) { }
    protected virtual void OnSplashComplete() { }
    protected virtual void OnResize() { }
    protected virtual void OnRestart() { }
    protected virtual void OnPerFrame() { }
    protected virtual void OnFixedStep(float deltaSeconds) { }
    protected virtual void OnRender() { }
    protected virtual string FrameTitle() => _config.WindowTitle;
    protected virtual int SpriteCount => 0;

    public void Run()
    {
        InitializeGame();
        SynchronizeViewport();
        RunSplash();
        _frameTimer = new FrameTimer(_config.FrameCap);
        OnSplashComplete();
        RunGame();
        ShutdownGame();
    }

    protected void Present(ReadOnlySpan<SpritePacket> sprites)
    {
        _renderer.BeginFrame(_viewport);
        _renderer.Submit(sprites);
        _renderer.EndFrame();
    }

    private void RunSplash()
    {
        if (!ShowSplash) return;
        FrameTimer splashTimer = new(_config.SplashFramesPerSecond);
        double elapsed = 0;
        while (!_window.ShouldClose && (!TexturesLoaded || elapsed < _config.SplashMinimumSeconds))
        {
            _window.PumpEvents();
            _input.Update();
            if (_input.WasPressed(GameKey.Escape)) _window.Close();
            if (_window.Size != _viewport)
            {
                _viewport = _window.Size;
                Camera.Resize(_viewport);
                OnResize();
            }
            OnSplashFrame(SplashPercent);
            splashTimer.WaitForNextFrame();
            elapsed += splashTimer.Advance();
        }
    }

    private void SynchronizeViewport()
    {
        _viewport = _window.Size;
        Camera.Resize(_viewport);
        OnResize();
    }

    private void RunGame()
    {
        while (!_window.ShouldClose)
        {
            SnapshotGcState();
            _window.PumpEvents();
            _input.Update();
            if (_input.WasPressed(GameKey.Fullscreen)) _window.SetFullscreen(!_window.Fullscreen);
            if (_input.WasPressed(GameKey.Restart)) OnRestart();
            if (_input.WasPressed(GameKey.Escape)) _window.Close();
            if (_window.IsMinimized)
            {
                _frameTimer.WaitForNextFrame();
                continue;
            }
            if (_window.Size != _viewport)
            {
                _viewport = _window.Size;
                Camera.Resize(_viewport);
                OnResize();
            }

            double elapsed = _frameTimer.Advance();
            _clock.Advance(elapsed);
            OnPerFrame();

            int fixedSteps = 0;
            long fixedBytes = 0;
            while (_clock.TryConsumeFixedStep())
            {
                fixedSteps++;
                long beforeFixed = GC.GetAllocatedBytesForCurrentThread();
                OnFixedStep((float)GameClock.FixedStep);
                fixedBytes += GC.GetAllocatedBytesForCurrentThread() - beforeFixed;
            }

            long beforeRender = GC.GetAllocatedBytesForCurrentThread();
            OnRender();
            long renderBytes = GC.GetAllocatedBytesForCurrentThread() - beforeRender;

            string title = FrameTitle();
            if (title != _lastTitle)
            {
                _window.SetTitle(title);
                _lastTitle = title;
            }
            _frameTimer.WaitForNextFrame();
            if (_config.ShowMetrics) RecordMetrics(elapsed, fixedSteps, fixedBytes, renderBytes);
        }
    }

    private void SnapshotGcState()
    {
        _frameStartAlloc = GC.GetAllocatedBytesForCurrentThread();
        _frameStartGen0 = GC.CollectionCount(0);
        _frameStartGen1 = GC.CollectionCount(1);
        _frameStartGen2 = GC.CollectionCount(2);
    }

    private void RecordMetrics(double elapsed, int fixedSteps, long fixedBytes, long renderBytes)
    {
        _metrics.Add(
            elapsed * 1000.0,
            fixedSteps,
            SpriteCount,
            GC.GetAllocatedBytesForCurrentThread() - _frameStartAlloc,
            fixedBytes,
            renderBytes,
            GC.CollectionCount(0) - _frameStartGen0,
            GC.CollectionCount(1) - _frameStartGen1,
            GC.CollectionCount(2) - _frameStartGen2);
    }
}
