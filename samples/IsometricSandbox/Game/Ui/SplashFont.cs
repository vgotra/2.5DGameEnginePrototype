using System.Numerics;
using Engine.App;
using Engine.Rendering;

namespace IsometricSandbox.Game.Ui;

public readonly record struct SplashGlyph(Vector2 UvOffset, Vector2 UvScale, float Advance, float Height, bool Visible);

public sealed class SplashFont : IDisposable
{
    private const int FirstCharacter = 32;
    private const int CharacterCount = 95;
    private const int AtlasColumnCount = 16;
    private const int AtlasRowCount = 6;
    private const float GlyphSize = 48f;
    private const float SpaceAdvance = 26f;
    private readonly SplashGlyph[] _glyphs = new SplashGlyph[CharacterCount];
    private readonly RenderContext? _renderer;

    public SplashFont(RenderContext renderer, string atlasPath)
    {
        PngImage? image = PngLoader.Decode(atlasPath);
        TextureHandle atlas = image.HasValue
            ? renderer.UploadTexture(image.Value.Data, image.Value.Width, image.Value.Height, TextureFilter.Linear)
            : default;
        _renderer = renderer;
        Atlas = atlas;
        for (int i = 0; i < _glyphs.Length; i++)
        {
            int column = i % AtlasColumnCount;
            int row = i / AtlasColumnCount;
            bool visible = i != (' ' - FirstCharacter);
            _glyphs[i] = CreateGlyph(column, row, visible);
        }
    }

    public SplashFont(TextureHandle atlas)
    {
        Atlas = atlas;
        for (int i = 0; i < _glyphs.Length; i++)
        {
            int column = i % AtlasColumnCount;
            int row = i / AtlasColumnCount;
            _glyphs[i] = CreateGlyph(column, row, i != (' ' - FirstCharacter));
        }
    }

    public TextureHandle Atlas { get; }

    public void Dispose()
    {
        if (_renderer is not null && Atlas.Value >= 0) _renderer.ReleaseTexture(Atlas);
    }

    public float GetAdvance(char character, float scale) => GetGlyph(character).Advance * scale;

    public SplashGlyph GetGlyph(char character)
    {
        int index = char.ToUpperInvariant(character) - FirstCharacter;
        if ((uint)index >= CharacterCount) index = '?' - FirstCharacter;
        return _glyphs[index];
    }

    public float MeasureWidth(ReadOnlySpan<char> text, float scale)
    {
        float width = 0f;
        for (int i = 0; i < text.Length; i++) width += GetGlyph(text[i]).Advance * scale;
        return text.Length == 0 ? 0f : width - 0.1f * scale;
    }

    private static SplashGlyph CreateGlyph(int column, int row, bool visible)
        => new(
            new Vector2((float)column / AtlasColumnCount, (float)row / AtlasRowCount),
            new Vector2(1f / AtlasColumnCount, 1f / AtlasRowCount),
            visible ? GlyphSize : SpaceAdvance,
            GlyphSize,
            visible);
}
