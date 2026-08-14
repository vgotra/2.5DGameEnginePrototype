using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public readonly record struct SplashGlyph(Vector2 UvOffset, Vector2 UvScale, float Advance, float Height, bool Visible);

public sealed class SplashFont
{
    private const int FirstCharacter = 32;
    private const int CharacterCount = 95;
    private readonly SplashGlyph[] _glyphs = new SplashGlyph[CharacterCount];

    public SplashFont(IRenderer renderer, string atlasPath)
    {
        TextureHandle atlas = PngLoader.Load(renderer, atlasPath, TextureFilter.Linear) ?? default;
        Atlas = atlas;
        for (int i = 0; i < _glyphs.Length; i++)
        {
            int column = i % 16;
            int row = i / 16;
            bool visible = i != (' ' - FirstCharacter);
            _glyphs[i] = new SplashGlyph(new Vector2(column / 16f, row / 6f), new Vector2(1f / 16f, 1f / 6f), visible ? 48f : 26f, 48f, visible);
        }
    }

    public SplashFont(TextureHandle atlas)
    {
        Atlas = atlas;
        for (int i = 0; i < _glyphs.Length; i++)
        {
            int column = i % 16;
            int row = i / 16;
            _glyphs[i] = new SplashGlyph(new Vector2(column / 16f, row / 6f), new Vector2(1f / 16f, 1f / 6f), i == (' ' - FirstCharacter) ? 26f : 48f, 48f, i != (' ' - FirstCharacter));
        }
    }

    public TextureHandle Atlas { get; }

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
}
