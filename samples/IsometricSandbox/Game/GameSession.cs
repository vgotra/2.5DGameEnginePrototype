using System.Diagnostics;
using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Platform.Desktop;
using Engine.Rendering;

namespace IsometricSandbox.Game;

// The runnable game session: wires the window, renderer, world, and input
// together, then drives the frame loop. Each step of a frame is one small
// private method so the flow reads top to bottom (see Run/Frame).
public sealed class GameSession : IDisposable
{
    private readonly Options _options;
    private readonly PlatformSession _session;
    private readonly IGameWindow _window;
    private readonly IInputState _input;
    private readonly ArcherGame _game;
    private readonly Player _player;
    private readonly IsometricCamera _camera;
    private readonly SceneRenderer _sceneRenderer;
    private readonly BitmapFont _font;
    private readonly SplashScreen _splash;
    private readonly SpritePacket[] _splashSprites = new SpritePacket[128];
    private GameClock _clock;
    private FrameTimer _frameTimer;
    private readonly double _frameCap;
    private const double SplashFramesPerSecond = 30;

    private Vector2 _viewport;
    private FrameMetrics _frameMetrics;
    private int _lastScore = -1;
    private long _frameStartAlloc, _frameStartGen0, _frameStartGen1, _frameStartGen2;

    public GameSession(Options options)
    {
        _options = options;
        _frameCap = options.FrameCap;
        _session = GamePlatform.CreateWindow(SampleConfig.WindowTitle, SampleConfig.WindowWidth, SampleConfig.WindowHeight);
        _window = _session.Window;
        _input = _session.Input;

        TileMap map = new();
        _sceneRenderer = new SceneRenderer(_window.NativeSurface, map, _window.Size);
        _font = new BitmapFont(_sceneRenderer.Renderer);
        _splash = new SplashScreen(_font, _window.Size);
        _game = new ArcherGame(map, SampleConfig.AnimalCount);
        _player = new Player(_game.PlayerStart);
        _camera = new IsometricCamera(_window.Size) { Mode = options.FlatMode ? GameMode.TopDown : GameMode.Isometric };
        _clock = new GameClock();
        _frameTimer = new FrameTimer(_frameCap);
        _viewport = _window.Size;
        if (options.StartFullscreen) _window.SetFullscreen(true);
    }

    public void Run()
    {
        RunSplash();
        // The splash paced its own frames; reset the timer so the first game
        // frame does not inherit a large elapsed value and burst simulation.
        _frameTimer = new FrameTimer(_frameCap);
        while (!_window.ShouldClose && !_input.IsDown(GameKey.Escape))
            Frame();
    }

    // Shows the splash at ~30 fps while the world textures load one at a
    // time, holding for at least SampleConfig.SplashMinimumSeconds.
    private void RunSplash()
    {
        Stopwatch timer = Stopwatch.StartNew();
        FrameTimer splashTimer = new(SplashFramesPerSecond);
        while (!_window.ShouldClose)
        {
            splashTimer.Advance();
            _window.PumpEvents();
            _input.Update();
            if (_window.Size != _viewport) UpdateViewport();
            if (_window.IsMinimized)
            {
                Thread.Sleep(15);
                continue;
            }

            if (!_sceneRenderer.TexturesLoaded) _sceneRenderer.LoadNextTexture();

            RenderSplash(SplashPercent());

            if (_sceneRenderer.TexturesLoaded && timer.Elapsed.TotalSeconds >= SampleConfig.SplashMinimumSeconds)
                break;

            splashTimer.WaitForNextFrame();
        }
    }

    private int SplashPercent()
    {
        int steps = Math.Max(1, _sceneRenderer.TextureSteps);
        return 5 + _sceneRenderer.TextureProgress * 95 / steps;
    }

    private void RenderSplash(int percent)
    {
        int count = _splash.Render(_splashSprites, SampleConfig.WindowTitle, percent);
        _sceneRenderer.Present(_splashSprites.AsSpan(0, count));
    }

    // One frame: events → input → simulate → render → pace → metrics.
    private void Frame()
    {
        SnapshotGcState();
        _window.PumpEvents();
        _input.Update();
        if (_input.WasPressed(GameKey.Fullscreen)) _window.SetFullscreen(!_window.Fullscreen);
        if (_input.WasPressed(GameKey.Restart)) Restart();
        if (_window.IsMinimized) { PauseWhileMinimized(); return; }
        if (_window.Size != _viewport) UpdateViewport();

        double elapsed = _frameTimer.Advance();
        _clock.Advance(elapsed);
        if (_input.MousePressed) _player.AimAt(_camera, _input.MousePosition, _game.Map);

        int fixedSteps = RunFixedSteps();
        _camera.Follow(_player.Position, _game.Map);
        int spriteCount = RenderFrame();
        UpdateScoreTitle();
        _frameTimer.WaitForNextFrame();
        if (_options.ShowMetrics) RecordFrameMetrics(elapsed, fixedSteps, spriteCount);
    }

    // R restarts the run: fresh animals, score, and player position.
    private void Restart()
    {
        _game.Reset();
        _player.Reset(_game.PlayerStart);
    }

    // While minimized there is nothing to simulate or draw; just pace.
    private void PauseWhileMinimized()
    {
        _frameTimer.WaitForNextFrame();
        if (_frameCap <= 0) Thread.Sleep(15);
    }

    // Keeps the camera and swapchain in sync with the window size.
    private void UpdateViewport()
    {
        _viewport = _window.Size;
        _camera.Resize(_viewport);
        _sceneRenderer.Resize(_viewport);
        _splash.Resize(_viewport);
    }

    // Runs one or more fixed simulation steps and returns how many ran.
    private int RunFixedSteps()
    {
        Vector2 direction = ReadMovementInput();
        int steps = 0;
        while (_clock.TryConsumeFixedStep())
        {
            steps++;
            _player.Step(_game.Map, direction, _input.WasPressed(GameKey.Space), (float)GameClock.FixedStep);
            if (_player.ConsumePendingShot(out Vector2 target)) _game.Shoot(_player.Position, target);
            _game.UpdateFixed(_player.Position, (float)GameClock.FixedStep);
        }
        return steps;
    }

    // Maps WASD/arrow keys to a movement direction.
    private Vector2 ReadMovementInput()
    {
        float right = (_input.IsDown(GameKey.Right) ? 1 : 0) - (_input.IsDown(GameKey.Left) ? 1 : 0);
        float down = (_input.IsDown(GameKey.Down) ? 1 : 0) - (_input.IsDown(GameKey.Up) ? 1 : 0);
        return new Vector2(right, down);
    }

    private int RenderFrame() => _sceneRenderer.SubmitFrame(
        _game.Map, _camera, _game.Animals, _game.Arrows.AsSpan(0, _game.ArrowCount),
        _player.Position, _player.JumpHeight);

    // The score is shown in the window title; only update on change.
    private void UpdateScoreTitle()
    {
        if (_game.Score == _lastScore) return;
        _lastScore = _game.Score;
        _window.SetTitle($"{SampleConfig.WindowTitle} — Score {_game.Score}");
    }

    // Prints a rolling metrics table every 120 frames when --metrics is set.
    private void RecordFrameMetrics(double elapsed, int fixedSteps, int spriteCount)
    {
        _frameMetrics.Add(
            elapsed * 1000.0,
            fixedSteps,
            spriteCount,
            GC.GetAllocatedBytesForCurrentThread() - _frameStartAlloc,
            GC.CollectionCount(0) - _frameStartGen0,
            GC.CollectionCount(1) - _frameStartGen1,
            GC.CollectionCount(2) - _frameStartGen2);
    }

    private void SnapshotGcState()
    {
        _frameStartAlloc = GC.GetAllocatedBytesForCurrentThread();
        _frameStartGen0 = GC.CollectionCount(0);
        _frameStartGen1 = GC.CollectionCount(1);
        _frameStartGen2 = GC.CollectionCount(2);
    }

    // Dispose order mirrors the old `using` declarations: the renderer is
    // torn down before the platform session that owns the window/surface.
    public void Dispose()
    {
        _sceneRenderer.Dispose();
        _session.Dispose();
    }
}
