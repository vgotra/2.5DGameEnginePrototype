using System.Numerics;
using Engine.App;
using Engine.Rendering;

namespace Engine.Tests;

internal static class RenderExtractionTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(ExtractTiles_IsoExtractionBounded), ExtractTiles_IsoExtractionBounded),
        new(nameof(ExtractTiles_IsoSpritesAreDiamonds), ExtractTiles_IsoSpritesAreDiamonds),
        new(nameof(ExtractTiles_FlatExtractionBounded), ExtractTiles_FlatExtractionBounded),
        new(nameof(ExtractTiles_FlatSpritesAreBoxes), ExtractTiles_FlatSpritesAreBoxes),
        new(nameof(ExtractTiles_CullsOffScreenTiles), ExtractTiles_CullsOffScreenTiles),
    ];

    private static TileGrid OpenGrid() => new(20, 20, 64, 32, new byte[400]);

    private static int Extract(TileGrid grid, IsometricCamera camera, SpritePacket[] sprites)
        => SpriteExtraction.ExtractTiles(grid, camera, null, null, sprites);

    private static void ExtractTiles_IsoExtractionBounded()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        int extracted = Extract(grid, camera, sprites);
        TestAssert.True(extracted > 0 && extracted <= grid.Width * grid.Height * 2, "iso map sprite extraction is bounded");
    }

    private static void ExtractTiles_IsoSpritesAreDiamonds()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        Extract(grid, camera, sprites);
        TestAssert.True(sprites[0].Shape == ShapeKind.Diamond && sprites[1].Shape == ShapeKind.Diamond, "iso sprites are diamonds");
    }

    private static void ExtractTiles_FlatExtractionBounded()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        int extracted = Extract(grid, camera, sprites);
        TestAssert.True(extracted > 0 && extracted <= grid.Width * grid.Height * 2, "flat map sprite extraction is bounded");
    }

    private static void ExtractTiles_FlatSpritesAreBoxes()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        Extract(grid, camera, sprites);
        TestAssert.True(sprites[0].Shape == ShapeKind.Box && sprites[1].Shape == ShapeKind.Box && sprites[1].Size == new Vector2(grid.TileWidth, grid.TileWidth), "flat sprites are boxes");
    }

    private static void ExtractTiles_CullsOffScreenTiles()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(300, 300)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        int extracted = Extract(grid, camera, sprites);
        TestAssert.True(extracted > 0 && extracted < grid.Width * grid.Height * 2, "viewport culling skips off-screen tiles");
    }
}
