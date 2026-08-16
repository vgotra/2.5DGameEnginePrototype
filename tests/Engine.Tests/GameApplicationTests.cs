using System.Numerics;
using Engine.App;
using Engine.Platform;
using Engine.Rendering;

namespace Engine.Tests;

internal static class GameApplicationTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(ApplicationOwnsLifecycleAndSafeRenderer), ApplicationOwnsLifecycleAndSafeRenderer),
    ];

    private static void ApplicationOwnsLifecycleAndSafeRenderer()
    {
        FakeBackend backend = new();
        GameApplicationOptions options = new("test", new Vector2(320, 200), 0, 32);
        using (GameApplication application = new(options, backend))
        {
            application.Run(new TestModule());

            TestAssert.True(backend.PumpCount > 0, "application owns backend event polling");
            TestAssert.True(backend.RendererBeginCount > 0, "module renders through the safe render context");
            TestAssert.True(backend.RendererSubmitCount == backend.RendererBeginCount, "each begun frame is submitted once");
            TestAssert.True(backend.RendererEndCount == backend.RendererBeginCount, "application closes each renderer frame");
        }
        TestAssert.True(backend.Disposed, "application owns backend disposal");
    }

    private sealed class TestModule : IGameModule
    {
        public void Initialize(GameContext context) => context.World.LoadScene("test");

        public void Update(GameContext context)
        {
        }

        public void Render(GameContext context)
        {
            context.Renderer.BeginFrame(new Vector2(320, 200));
            context.Renderer.EndFrame();
        }

        public void Shutdown(GameContext context)
        {
        }
    }

    private sealed class FakeBackend : IGameApplicationBackend
    {
        private readonly FakeRenderer _renderer = new();
        private readonly FakeInput _input = new();

        public Vector2 Viewport => new(320, 200);
        public bool ShouldClose { get; private set; }
        public bool IsMinimized => false;
        public bool Fullscreen => false;
        public IGameInput Input => _input;
        public RenderContext Renderer { get; }
        public int PumpCount { get; private set; }
        public int RendererBeginCount => _renderer.BeginCount;
        public int RendererSubmitCount => _renderer.SubmitCount;
        public int RendererEndCount => _renderer.EndCount;
        public bool Disposed { get; private set; }

        public FakeBackend() => Renderer = new RenderContext(_renderer, 32);

        public void PumpEvents()
        {
            PumpCount++;
            if (PumpCount >= 2) ShouldClose = true;
        }

        public void SetFullscreen(bool fullscreen)
        {
        }

        public void SetTitle(string title)
        {
        }

        public void Close() => ShouldClose = true;
        public void ResizeRenderer(int width, int height) { }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Renderer.Dispose();
        }
    }

    private sealed class FakeInput : IGameInput
    {
        public bool IsDown(GameKey key) => false;
        public bool WasPressed(GameKey key) => false;
        public bool WasReleased(GameKey key) => false;
        public bool IsMouseButtonDown(MouseButton button) => false;
        public bool WasMouseButtonPressed(MouseButton button) => false;
        public Vector2 MousePosition => Vector2.Zero;
    }

    private sealed class FakeRenderer : IRenderer
    {
        public int BeginCount { get; private set; }
        public int SubmitCount { get; private set; }
        public int EndCount { get; private set; }

        public void BeginFrame(Vector2 viewport) => BeginCount++;
        public void Submit(ReadOnlySpan<SpritePacket> sprites) => SubmitCount++;
        public void EndFrame() => EndCount++;
        public TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter) => new(1);
        public bool ReleaseTexture(TextureHandle texture) => true;
        public void Resize(int width, int height) { }
        public void Dispose() { }
    }
}
