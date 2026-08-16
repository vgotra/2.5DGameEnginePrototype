using System.Numerics;
using Engine.Rendering;
using IsometricSandbox.Game.Configuration;

namespace IsometricSandbox.Game.Ui;

public sealed class SplashScreen(SplashFont font, Vector2 viewport)
{
    private const int MinimumPercent = 0;
    private const int MaximumPercent = 100;
    private const float Half = 0.5f;
    private const float AvailableWidthRatio = 0.7f;
    private const float BarHeight = 16f;
    private const float BarInset = 3f;
    private const float BarVerticalPosition = 0.62f;
    private const float TitleVerticalPosition = 0.42f;
    private const float PercentageVerticalPosition = 0.7f;
    private const float EmptyGlyphHeight = 48f;

    private static readonly Vector4 Backdrop = new(0, 0, 0, 1);
    private static readonly Vector4 Track = new(0.18f, 0.18f, 0.2f, 1);
    private static readonly Vector4 Fill = new(0.25f, 0.75f, 0.35f, 1);
    private static readonly Vector4 White = new(1, 1, 1, 1);

    private Vector2 _viewport = viewport;

    public int LastVisibleGlyphCount { get; private set; }
    public int LastPercentageGlyphCount { get; private set; }

    public void Resize(Vector2 viewport) => _viewport = viewport;

    public int Render(Span<SpritePacket> sprites, string title, int percent)
    {
        percent = Math.Clamp(percent, MinimumPercent, MaximumPercent);
        int written = 0;
        LastVisibleGlyphCount = 0;
        LastPercentageGlyphCount = 0;
        Vector2 center = _viewport * Half;

        float availableViewportWidth = _viewport.X * AvailableWidthRatio;
        float barWidth = Math.Min(SampleConfig.SplashBarMaxWidth, availableViewportWidth);
        Vector2 barCenter = new(center.X, _viewport.Y * BarVerticalPosition);
        float barInsetWidth = BarInset * 2f;
        float fillableBarWidth = Math.Max(0f, barWidth - barInsetWidth);
        float fillWidth = fillableBarWidth * ((float)percent / MaximumPercent);

        if (written < sprites.Length) sprites[written++] = new SpritePacket(center, _viewport, Backdrop, default, default, 0);
        if (written < sprites.Length) sprites[written++] = new SpritePacket(barCenter, new(barWidth, BarHeight), Track, default, default, 1);
        if (fillWidth > 0f && written < sprites.Length)
            sprites[written++] = new SpritePacket(new(barCenter.X - barWidth * Half + fillWidth * Half, barCenter.Y), new(fillWidth, BarHeight - barInsetWidth), Fill, default, default, 2);

        float titleWidth = font.MeasureWidth(title, 1f);
        float maxTextWidth = barWidth * SampleConfig.SplashTextWidthMultiplier;
        float titleScale = titleWidth <= 0f ? 1f : Math.Min(SampleConfig.SplashTitleMaxScale, maxTextWidth / titleWidth);
        written = WriteText(sprites, written, title, new Vector2(center.X, _viewport.Y * TitleVerticalPosition), titleScale, 3, false);
        string percentageText = percent + "%";
        float percentageWidth = font.MeasureWidth(percentageText, 1f);
        float percentageScale = percentageWidth <= 0f ? 1f : Math.Min(SampleConfig.SplashPercentageMaxScale, maxTextWidth / percentageWidth);
        written = WriteText(sprites, written, percentageText, new Vector2(center.X, _viewport.Y * PercentageVerticalPosition), percentageScale, 3, true);
        return written;
    }

    private int WriteText(Span<SpritePacket> sprites, int written, string text, Vector2 center, float scale, float sortKey, bool percentage)
    {
        float measuredWidth = font.MeasureWidth(text, scale);
        float halfWidth = measuredWidth * Half;
        float availableViewportWidth = Math.Max(0f, _viewport.X - measuredWidth);
        float left = Math.Clamp(center.X - halfWidth, 0f, availableViewportWidth);
        float cursor = left;
        float lineTop = center.Y - Half * GlyphHeight(text) * scale;
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

    private float GlyphHeight(string text) => text.Length == 0 ? EmptyGlyphHeight : font.GetGlyph(text[0]).Height;
}
