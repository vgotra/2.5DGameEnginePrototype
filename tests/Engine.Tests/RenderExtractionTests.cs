using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class RenderExtractionTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(ExtractTiles_IsoExtractionBounded), ExtractTiles_IsoExtractionBounded),
        new(nameof(ExtractTiles_IsoSpritesAreDiamonds), ExtractTiles_IsoSpritesAreDiamonds),
        new(nameof(ExtractTiles_CullsOffScreenTiles), ExtractTiles_CullsOffScreenTiles),
        new(nameof(EntityRenderBody_ExcludesPlayer), EntityRenderBody_ExcludesPlayer),
    ];

    private static TerrainSurface OpenGrid() => new(20, 20, 1f, 64f, 32f, 7);

    private static int Extract(TerrainSurface grid, IsometricCamera camera, SpritePacket[] sprites)
        => SpriteExtraction.ExtractTiles(grid, camera, null, null, sprites);

    private static void ExtractTiles_IsoExtractionBounded()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        int extracted = Extract(grid, camera, sprites);
        TestAssert.True(extracted > 0 && extracted <= grid.Width * grid.Height * 2, "iso map sprite extraction is bounded");
    }

    private static void ExtractTiles_IsoSpritesAreDiamonds()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        Extract(grid, camera, sprites);
        TestAssert.True(sprites[0].Size == new Vector2(grid.TileWidth + SpriteExtraction.BorderWidth * 2, grid.TileHeight + SpriteExtraction.BorderWidth * 2), "iso tile border preserves diamond dimensions");
    }

    private static void ExtractTiles_CullsOffScreenTiles()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(300, 300));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        int extracted = Extract(grid, camera, sprites);
        TestAssert.True(extracted > 0 && extracted < grid.Width * grid.Height * 2, "viewport culling skips off-screen tiles");
    }

    private static void EntityRenderBody_ExcludesPlayer()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        Entity player = new(4, 1);
        SpritePacket[] sprites = new SpritePacket[4];
        EntityRenderBody body = new() { Grid = grid, Camera = camera, Sprites = sprites, ExcludedEntity = player };
        Position position = new(new Vector2(10.5f, 10.5f));
        Renderable renderable = new(default, new Vector2(44, 56), Vector4.One);
        EntityRenderBody.Execute(ref body, player, ref position, ref renderable);
        TestAssert.True(body.Written == 0, "generic entity rendering excludes the manually rendered player");

        EntityRenderBody.Execute(ref body, new Entity(5, 1), ref position, ref renderable);
        TestAssert.True(body.Written == 2, "generic entity rendering keeps non-player entities");
    }
}
