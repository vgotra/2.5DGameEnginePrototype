using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

// The startup splash: a full-window backdrop, the game title, a progress bar,
// and a "NN%" readout. Everything is emitted as screen-space box sprites so it
// renders through the same batch path as the game.
public sealed class SplashScreen
{
    private static readonly Vector4 Backdrop = new(0, 0, 0, 1);
    private static readonly Vector4 Track = new(0.18f, 0.18f, 0.2f, 1);
    private static readonly Vector4 Fill = new(0.25f, 0.75f, 0.35f, 1);
    private static readonly Vector4 White = new(1, 1, 1, 1);

    private readonly BitmapFont _font;
    private Vector2 _viewport;

    public SplashScreen(BitmapFont font, Vector2 viewport)
    {
        _font = font;
        _viewport = viewport;
    }

    public void Resize(Vector2 viewport) => _viewport = viewport;

    // Writes the splash sprites into the caller's span and returns how many
    // were written. The bar fills left-to-right as percent goes 0..100.
    public int Render(Span<SpritePacket> sprites, string title, int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        int written = 0;
        Vector2 center = _viewport * 0.5f;

        float barWidth = Math.Min(480f, _viewport.X * 0.7f);
        const float barHeight = 16f;
        const float barInset = 3f;
        Vector2 barCenter = new(center.X, _viewport.Y * 0.62f);
        float fillWidth = Math.Max(0f, barWidth - barInset * 2f) * (percent / 100f);

        sprites[written++] = new SpritePacket(center, _viewport, Backdrop, default, default, 0, ShapeKind.Box);
        sprites[written++] = new SpritePacket(barCenter, new(barWidth, barHeight), Track, default, default, 1, ShapeKind.Box);
        sprites[written++] = new SpritePacket(new(barCenter.X - barWidth * 0.5f + fillWidth * 0.5f, barCenter.Y), new(fillWidth, barHeight - barInset * 2f), Fill, default, default, 2, ShapeKind.Box);

        float titleScale = Math.Min(3f, (barWidth * 0.9f) / _font.MeasureWidth(title, 1f));
        written = WriteText(sprites, written, title, new Vector2(center.X, _viewport.Y * 0.42f), titleScale, 3);
        written = WriteText(sprites, written, percent + "%", new Vector2(center.X, _viewport.Y * 0.7f), 2f, 3);
        return written;
    }

    private int WriteText(Span<SpritePacket> sprites, int written, string text, Vector2 center, float scale, float sortKey)
    {
        float cursor = center.X - _font.MeasureWidth(text, scale) * 0.5f;
        float lineTop = center.Y - BitmapFont.GlyphRows * scale * 0.5f;
        foreach (char c in text)
        {
            BitmapGlyph glyph = _font.GetGlyph(c);
            float width = glyph.Columns * scale;
            float height = glyph.Rows * scale;
            float y = lineTop + (BitmapFont.GlyphRows - glyph.Rows) * scale * 0.5f + height * 0.5f;
            sprites[written++] = new SpritePacket(
                new Vector2(cursor + width * 0.5f, y), new Vector2(width, height),
                White, glyph.Texture, default, sortKey, ShapeKind.Box);
            cursor += (glyph.Columns + BitmapFont.Spacing) * scale;
        }
        return written;
    }
}
