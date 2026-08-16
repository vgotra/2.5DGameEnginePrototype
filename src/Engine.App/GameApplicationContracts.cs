using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Rendering;
using Engine.Threading;

namespace Engine.App;

public readonly record struct GameApplicationOptions(
    string WindowTitle,
    Vector2 Resolution,
    double FrameCap = 0,
    int SpriteCapacity = 16_384,
    bool StartFullscreen = false,
    bool ShowMetrics = false);

public interface IGameInput
{
    bool IsDown(GameKey key);
    bool WasPressed(GameKey key);
    bool WasReleased(GameKey key);
    bool IsMouseButtonDown(MouseButton button);
    bool WasMouseButtonPressed(MouseButton button);
    Vector2 MousePosition { get; }
}

public interface IGameApplicationBackend : IDisposable
{
    Vector2 Viewport { get; }
    bool ShouldClose { get; }
    bool IsMinimized { get; }
    bool Fullscreen { get; }
    IGameInput Input { get; }
    RenderContext Renderer { get; }
    void PumpEvents();
    void SetFullscreen(bool fullscreen);
    void SetTitle(string title);
    void Close();
    void ResizeRenderer(int width, int height);
}

internal interface IEngineHostBackend : IGameApplicationBackend
{
    IGameWindow Window { get; }
    IInputState RawInput { get; }
    IRenderer RawRenderer { get; }
    JobSystem Jobs { get; }
}

public interface IGameModule
{
    void Initialize(GameContext context);
    void Update(GameContext context);
    void Render(GameContext context);
    void Shutdown(GameContext context);
}

public sealed class GameContext
{
    internal GameContext(World world, IGameApplicationBackend backend, GameClock clock)
    {
        World = world;
        Backend = backend;
        Clock = clock;
    }

    public World World { get; }
    public Scene? ActiveScene => World.ActiveScene;
    public WorldMap Map => World.Map;
    public IGameInput Input => Backend.Input;
    public RenderContext Renderer => Backend.Renderer;
    public GameClock Clock { get; }

    internal IGameApplicationBackend Backend { get; }
}
