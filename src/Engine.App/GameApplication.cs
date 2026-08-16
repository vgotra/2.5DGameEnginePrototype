using System.Diagnostics;
using System.Numerics;
using Engine.Core;
using Engine.Platform;

namespace Engine.App;

public sealed class GameApplication : IDisposable
{
    private readonly IGameApplicationBackend _backend;
    private readonly GameClock _clock = new();
    private readonly FrameTimer _frameTimer;
    private readonly World _world;
    private readonly GameContext _context;
    private bool _disposed;

    public GameApplication(GameApplicationOptions options, IGameApplicationBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);
        if (options.Resolution.X <= 0 || options.Resolution.Y <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Application resolution must be positive.");
        if (options.SpriteCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Sprite capacity must be positive.");
        _backend = backend;
        _frameTimer = new FrameTimer(options.FrameCap);
        _world = new World(options.WindowTitle);
        _context = new GameContext(_world, backend, _clock);
    }

    public World World => _world;
    public GameContext Context => _context;

    public void Run(IGameModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        ThrowIfDisposed();
        module.Initialize(_context);
        try
        {
            Vector2 previousViewport = _backend.Viewport;
            while (!_backend.ShouldClose)
            {
                _backend.PumpEvents();
                Vector2 currentViewport = _backend.Viewport;
                if (currentViewport != previousViewport)
                {
                    _context.Renderer.Resize((int)currentViewport.X, (int)currentViewport.Y);
                    previousViewport = currentViewport;
                }
                if (_backend.Input.WasPressed(GameKey.Escape)) _backend.Close();
                if (_backend.Input.WasPressed(GameKey.Fullscreen)) _backend.SetFullscreen(!_backend.Fullscreen);
                if (_backend.IsMinimized)
                {
                    _frameTimer.WaitForNextFrame();
                    continue;
                }

                _clock.Advance(_frameTimer.Advance());
                while (_clock.TryConsumeFixedStep())
                {
                    module.Update(_context);
                    _world.ApplyCommands();
                }

                module.Render(_context);
                _context.Renderer.Present();
                _frameTimer.WaitForNextFrame();
            }
        }
        finally
        {
            module.Shutdown(_context);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _backend.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GameApplication));
    }
}
