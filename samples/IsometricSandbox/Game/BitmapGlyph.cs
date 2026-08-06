using Engine.Rendering;

namespace IsometricSandbox.Game;

// One character of the procedural bitmap font: its texture and pixel-cell size.
public readonly record struct BitmapGlyph(TextureHandle Texture, int Columns, int Rows);
