using System.Numerics;
using Engine.App;
using Engine.Platform.SDL3;
using Engine.Rendering;
using Engine.Rendering.Vulkan;
using Engine.Threading;

namespace Engine.Platform.Desktop;

internal sealed class DesktopGameBackend : IEngineHostBackend
{
    private readonly SdlWindow _window;
    private readonly SdlInput _input;
    private readonly VulkanRenderer _renderer;
    private readonly JobSystem _jobs;
    private readonly DesktopInput _gameInput;
    private bool _disposed;

    private DesktopGameBackend(
        SdlWindow window,
        SdlInput input,
        VulkanRenderer renderer,
        JobSystem jobs,
        int spriteCapacity)
    {
        _window = window;
        _input = input;
        _renderer = renderer;
        _jobs = jobs;
        _gameInput = new DesktopInput(input);
        Renderer = new RenderContext(renderer, spriteCapacity);
    }

    public Vector2 Viewport => _window.Size;
    public bool ShouldClose => _window.ShouldClose;
    public bool IsMinimized => _window.IsMinimized;
    public bool Fullscreen => _window.Fullscreen;
    public IGameInput Input => _gameInput;
    public RenderContext Renderer { get; }
    IGameWindow IEngineHostBackend.Window => _window;
    IInputState IEngineHostBackend.RawInput => _input;
    IRenderer IEngineHostBackend.RawRenderer => _renderer;
    JobSystem IEngineHostBackend.Jobs => _jobs;

    internal static DesktopGameBackend Create(GameApplicationOptions options)
    {
        SdlWindow window = new((int)options.Resolution.X, (int)options.Resolution.Y, options.WindowTitle);
        SdlInput input = new(window);
        JobSystem jobs = new();
        try
        {
            VulkanRenderer renderer = new(window.NativeSurface, jobs);
            DesktopGameBackend backend = new(window, input, renderer, jobs, options.SpriteCapacity);
            if (options.StartFullscreen) backend.SetFullscreen(true);
            return backend;
        }
        catch
        {
            jobs.Dispose();
            window.Dispose();
            throw;
        }
    }

    public void PumpEvents()
    {
        _window.PumpEvents();
        _input.Update();
    }

    public void SetFullscreen(bool fullscreen) => _window.SetFullscreen(fullscreen);
    public void SetTitle(string title) => _window.SetTitle(title);
    public void Close() => _window.Close();
    public void ResizeRenderer(int width, int height) => _renderer.Resize(width, height);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Renderer.Dispose();
        _jobs.Dispose();
        _window.Dispose();
    }

    private sealed class DesktopInput(IInputState input) : IGameInput
    {
        public bool IsDown(GameKey key) => input.IsDown(key);
        public bool WasPressed(GameKey key) => input.WasPressed(key);
        public bool WasReleased(GameKey key) => input.WasReleased(key);
        public bool IsMouseButtonDown(MouseButton button) => input.IsMouseButtonDown(button);
        public bool WasMouseButtonPressed(MouseButton button) => input.WasMouseButtonPressed(button);
        public Vector2 MousePosition => input.MousePosition;
    }
}
