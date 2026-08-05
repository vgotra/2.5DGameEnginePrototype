# Asset Workflow

**Reference** — how to supply your own art for the mini-game.

## Where to put assets

Drop PNG files into `assets/textures/` at the repo root. The sample project copies
every `*.png` there into the build output `textures/` folder (`PreserveNewest`), so a
rebuild picks up new or changed files automatically.

Expected names (all optional — a missing file falls back to procedural/colored art
and is logged as `[assets] missing ...` on startup):

| File | Used for |
| --- | --- |
| `player.png` | The archer (upright quad). Fallback: procedural Ukraine flag. |
| `deer.png` | Deer animals. Fallback: brown blob. |
| `rabbit.png` | Rabbit animals. Fallback: gray blob. |
| `grass.png` | Walkable floor tiles. Fallback: green color. |
| `water.png` | River tiles. Fallback: blue color. |
| `tree.png` | Forest tiles. Fallback: dark-green color. |
| `bonfire.png` | Bonfire tile (tinted per-frame for flicker). Fallback: orange color. |
| `wall.png` | Border wall tiles. Fallback: gray color. |

`tools/GeneratePlaceholderTextures.ps1` writes simple placeholder PNGs (PowerShell +
System.Drawing, Windows) so the pipeline is testable before you add real art; existing
files are skipped unless you pass `-Force`.

## Rules for your art

- **PNG, RGBA** — transparency is alpha-blended (an entity quad shows only the opaque
  pixels of your image).
- **Pixel art** — textures upload with `TextureFilter.Nearest` (crisp) by default.
- **Entity sprites** should have the character roughly filling a portrait quad;
  the sprite is bottom-center anchored to its tile, and upright quads are used in both
  iso and `--2d` so square art is not diamond-clipped.
- **Tile art** is mapped onto the tile shape (diamond in iso, square in `--2d`); a
  square texture is clipped to the diamond.

## Engine plumbing

- `PngLoader.Load(IRenderer, path, filter)` in `Engine.Rendering` decodes a PNG
  (StbImageSharp) and uploads it, returning a `TextureHandle` (or `null` on failure).
- `IRenderer.UploadTexture(..., TextureFilter)` controls nearest vs linear filtering.
- `TextureLibrary` (sample) loads the expected names at startup and exposes the
  handles used by `RenderExtractionSystem`.

## Scope notes

Current limits: textures are uploaded once at startup (no per-frame uploads,
atlases, or bindless); loading is synchronous and decodes into a managed buffer.
The `docs/Conventions/Assets.md` design targets (unmanaged decode, async I/O, atlases)
remain future work.
