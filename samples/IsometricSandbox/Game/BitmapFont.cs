using Engine.Rendering;

namespace IsometricSandbox.Game;

// A tiny procedural bitmap font (5x7 pixel cells) used by the splash screen.
// Each glyph is rendered from a public-domain 5x7 pattern into its own tight
// texture, so text is drawn as a row of diamond sprites — one per character.
public sealed class BitmapFont
{
    public const int GlyphRows = 7;
    public const float Spacing = 1f;
    private const int GlyphPadding = 1;

    private readonly Dictionary<char, BitmapGlyph> _glyphs = new();
    private readonly BitmapGlyph _fallback;

    public BitmapFont(IRenderer renderer)
    {
        foreach ((char ch, string[] rows) in FontPatterns)
            _glyphs[ch] = BuildGlyph(renderer, rows);
        _fallback = _glyphs[' '];
    }

    public BitmapGlyph GetGlyph(char c)
        => _glyphs.TryGetValue(char.ToUpperInvariant(c), out BitmapGlyph glyph) ? glyph : _fallback;

    // Total on-screen width of text at the given scale, including spacing.
    public float MeasureWidth(ReadOnlySpan<char> text, float scale)
    {
        float width = 0f;
        foreach (char c in text)
        {
            BitmapGlyph glyph = GetGlyph(c);
            width += (glyph.Columns + Spacing) * scale;
        }
        return width == 0f ? 0f : width - Spacing * scale;
    }

    // Renders a glyph pattern into a tightly-bounded texture; blank patterns
    // (the space) become a 1x1 transparent pixel that draws nothing.
    private static BitmapGlyph BuildGlyph(IRenderer renderer, string[] rows)
    {
        int minColumn = int.MaxValue, maxColumn = -1, minRow = int.MaxValue, maxRow = -1;
        for (int row = 0; row < rows.Length; row++)
            for (int column = 0; column < rows[row].Length; column++)
                if (rows[row][column] == '#')
                {
                    minColumn = Math.Min(minColumn, column);
                    maxColumn = Math.Max(maxColumn, column);
                    minRow = Math.Min(minRow, row);
                    maxRow = Math.Max(maxRow, row);
                }

        if (maxColumn < minColumn)
            return new BitmapGlyph(renderer.UploadTexture(new byte[4], 1, 1, TextureFilter.Nearest), 3, GlyphRows, false);

        int width = maxColumn - minColumn + 1 + GlyphPadding * 2;
        int height = maxRow - minRow + 1 + GlyphPadding * 2;
        byte[] rgba = new byte[width * height * 4];
        for (int row = minRow; row <= maxRow; row++)
            for (int column = minColumn; column <= maxColumn; column++)
            {
                bool on = rows[row][column] == '#';
                int i = ((row - minRow + GlyphPadding) * width + (column - minColumn + GlyphPadding)) * 4;
                rgba[i] = 255;
                rgba[i + 1] = 255;
                rgba[i + 2] = 255;
                rgba[i + 3] = on ? (byte)255 : (byte)0;
            }
        TextureHandle texture = renderer.UploadTexture(rgba, width, height, TextureFilter.Nearest);
        return new BitmapGlyph(texture, width, height);
    }

    // 5x7 patterns: one entry per character, 7 rows of 5 cells.
    private static readonly (char Char, string[] Rows)[] FontPatterns =
    {
        ('A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }),
        ('B', new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." }),
        ('C', new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." }),
        ('D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." }),
        ('E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" }),
        ('F', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." }),
        ('G', new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." }),
        ('H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" }),
        ('I', new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." }),
        ('J', new[] { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." }),
        ('K', new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" }),
        ('L', new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" }),
        ('M', new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" }),
        ('N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" }),
        ('O', new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." }),
        ('P', new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." }),
        ('Q', new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" }),
        ('R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" }),
        ('S', new[] { ".###.", "#...#", "#....", ".###.", "....#", "#...#", ".###." }),
        ('T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." }),
        ('U', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." }),
        ('V', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." }),
        ('W', new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" }),
        ('X', new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" }),
        ('Y', new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." }),
        ('Z', new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" }),
        ('0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." }),
        ('1', new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." }),
        ('2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" }),
        ('3', new[] { "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###." }),
        ('4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." }),
        ('5', new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." }),
        ('6', new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." }),
        ('7', new[] { "#####", "....#", "...#.", "..#..", "..#..", "..#..", "..#.." }),
        ('8', new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." }),
        ('9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." }),
        ('%', new[] { "##..#", "##.#.", "...#.", "..#..", ".#...", "#.##.", "#..##" }),
        ('.', new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." }),
        (' ', new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." }),
    };
}
