using System.Numerics;
using Engine.Rendering;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class SplashScreenTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Font_ResolvesFallbackAndPaddedGlyphs), Font_ResolvesFallbackAndPaddedGlyphs),
        new(nameof(Font_MeasureWidthPreservesSpacing), Font_MeasureWidthPreservesSpacing),
        new(nameof(Splash_OrdersBackgroundBarAndText), Splash_OrdersBackgroundBarAndText),
        new(nameof(Splash_HandlesEmptyTitleAndZeroProgress), Splash_HandlesEmptyTitleAndZeroProgress),
    ];

    private static void Font_ResolvesFallbackAndPaddedGlyphs()
    {
        TestRenderer renderer = new();
        BitmapFont font = new(renderer);
        BitmapGlyph glyph = font.GetGlyph('A');
        BitmapGlyph fallback = font.GetGlyph('?');
        TestAssert.True(glyph.IsVisible && glyph.Columns > 5 && glyph.Rows > 7, "glyph has diamond-safe transparent padding");
        TestAssert.True(!fallback.IsVisible, "unknown glyph resolves to invisible space fallback");
        TestAssert.True(renderer.Uploads > 0, "font uploads procedural glyph textures");
    }

    private static void Font_MeasureWidthPreservesSpacing()
    {
        BitmapFont font = new(new TestRenderer());
        float one = font.MeasureWidth("A", 2f);
        float two = font.MeasureWidth("AA", 2f);
        float spaced = font.MeasureWidth("A A", 2f);
        TestAssert.True(two > one && spaced > two, "font metrics preserve glyph and space advances");
        TestAssert.True(font.MeasureWidth(ReadOnlySpan<char>.Empty, 2f) == 0f, "empty text measures to zero");
    }

    private static void Splash_OrdersBackgroundBarAndText()
    {
        SplashScreen splash = new(new BitmapFont(new TestRenderer()), new Vector2(800, 600));
        SpritePacket[] packets = new SpritePacket[32];
        int count = splash.Render(packets, "TEST", 50);
        TestAssert.True(count > 3, "splash emits title and percentage glyphs");
        TestAssert.True(packets[0].SortKey < packets[1].SortKey && packets[1].SortKey < packets[2].SortKey, "splash background and bar ordering is stable");
        TestAssert.True(packets[3].SortKey == 3f, "splash text is above the bar");
    }

    private static void Splash_HandlesEmptyTitleAndZeroProgress()
    {
        SplashScreen splash = new(new BitmapFont(new TestRenderer()), new Vector2(800, 600));
        SpritePacket[] packets = new SpritePacket[32];
        int count = splash.Render(packets, string.Empty, 0);
        TestAssert.True(count == 4, "zero progress omits the degenerate fill and empty title glyphs");
    }

    private sealed class TestRenderer : IRenderer
    {
        public int Uploads { get; private set; }

        public void BeginFrame(Vector2 viewport) { }
        public void Submit(ReadOnlySpan<SpritePacket> sprites) { }
        public void EndFrame() { }
        public TextureHandle UploadTexture(ReadOnlySpan<byte> rgba, int width, int height, TextureFilter filter = TextureFilter.Linear)
        {
            Uploads++;
            return new TextureHandle(Uploads);
        }
        public void Resize(int width, int height) { }
        public void Dispose() { }
    }
}
