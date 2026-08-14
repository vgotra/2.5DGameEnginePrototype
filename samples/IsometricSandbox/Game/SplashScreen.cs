using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

// The startup splash: a full-window backdrop, the game title, a progress bar,
// and a "NN%" readout. Everything is emitted as screen-space diamond sprites so it
// renders through the same batch path as the game.
public sealed class SplashScreen(SplashFont font, Vector2 viewport)
{
    private static readonly Vector4 Backdrop = new(0, 0, 0, 1);
    private static readonly Vector4 Track = new(0.18f, 0.18f, 0.2f, 1);
    private static readonly Vector4 Fill = new(0.25f, 0.75f, 0.35f, 1);
    private static readonly Vector4 White = new(1, 1, 1, 1);

    private Vector2 _viewport = viewport;

    public int LastVisibleGlyphCount { get; private set; }
    public int LastPercentageGlyphCount { get; private set; }

    public void Resize(Vector2 viewport) => _viewport = viewport;

    // Writes the splash sprites into the caller's span and returns how many
    // were written. The bar fills left-to-right as percent goes 0..100.
    public int Render(Span<SpritePacket> sprites, string title, int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        int written = 0;
        LastVisibleGlyphCount = 0;
        LastPercentageGlyphCount = 0;
        Vector2 center = _viewport * 0.5f;

        float availableViewportWidth = _viewport.X * 0.7f;
        float barWidth = Math.Min(SampleConfig.SplashBarMaxWidth, availableViewportWidth);
        const float barHeight = 16f;
        const float barInset = 3f;
        Vector2 barCenter = new(center.X, _viewport.Y * 0.62f);
        float barInsetWidth = barInset * 2f;
        float fillableBarWidth = Math.Max(0f, barWidth - barInsetWidth);
        float fillWidth = fillableBarWidth * (percent / 100f);

        if (written < sprites.Length) sprites[written++] = new SpritePacket(center, _viewport, Backdrop, default, default, 0);
        if (written < sprites.Length) sprites[written++] = new SpritePacket(barCenter, new(barWidth, barHeight), Track, default, default, 1);
        if (fillWidth > 0f && written < sprites.Length)
            sprites[written++] = new SpritePacket(new(barCenter.X - barWidth * 0.5f + fillWidth * 0.5f, barCenter.Y), new(fillWidth, barHeight - barInsetWidth), Fill, default, default, 2);

        float titleWidth = font.MeasureWidth(title, 1f);
        float maxTextWidth = barWidth * SampleConfig.SplashTextWidthMultiplier;
        float titleScale = titleWidth <= 0f ? 1f : Math.Min(SampleConfig.SplashTitleMaxScale, maxTextWidth / titleWidth);
        written = WriteText(sprites, written, title, new Vector2(center.X, _viewport.Y * 0.42f), titleScale, 3, false);
        string percentageText = percent + "%";
        float percentageWidth = font.MeasureWidth(percentageText, 1f);
        float percentageScale = percentageWidth <= 0f ? 1f : Math.Min(SampleConfig.SplashPercentageMaxScale, maxTextWidth / percentageWidth);
        written = WriteText(sprites, written, percentageText, new Vector2(center.X, _viewport.Y * 0.7f), percentageScale, 3, true);
        return written;
    }

    private int WriteText(Span<SpritePacket> sprites, int written, string text, Vector2 center, float scale, float sortKey, bool percentage)
    {
        float measuredWidth = font.MeasureWidth(text, scale);
        float halfWidth = measuredWidth * 0.5f;
        float availableViewportWidth = Math.Max(0f, _viewport.X - measuredWidth);
        float left = Math.Clamp(center.X - halfWidth, 0f, availableViewportWidth);
        float cursor = left;
        float lineTop = center.Y - 0.5f * glyphHeight(text) * scale;
        foreach (char c in text)
        {
            SplashGlyph glyph = font.GetGlyph(c);
            if (!glyph.Visible)
            {
                cursor += glyph.Advance * scale;
                continue;
            }
            float width = glyph.Advance * scale;
            float height = glyph.Height * scale;
            float y = lineTop + height * 0.5f;
            if (written >= sprites.Length) break;
            sprites[written++] = new SpritePacket(
                new Vector2(cursor + width * 0.5f, y), new Vector2(width, height),
                White, font.Atlas, default, sortKey) { UvScale = glyph.UvScale, UvOffset = glyph.UvOffset };
            if (percentage) LastPercentageGlyphCount++;
            else LastVisibleGlyphCount++;
            cursor += glyph.Advance * scale;
        }
        return written;
    }

    private float glyphHeight(string text) => text.Length == 0 ? 48f : font.GetGlyph(text[0]).Height;
}
