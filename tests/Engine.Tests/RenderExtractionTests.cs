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
        new(nameof(ExtractTiles_TexturedTilesUseLighterMapTint), ExtractTiles_TexturedTilesUseLighterMapTint),
        new(nameof(ExtractTiles_FallbackColorsAreLighter), ExtractTiles_FallbackColorsAreLighter),
        new(nameof(MapPalette_UsesNaturalGrassWaterAndFireColors), MapPalette_UsesNaturalGrassWaterAndFireColors),
        new(nameof(TerrainFill_UsesSolidColorsAndBonfireFlickerPreservesHue), TerrainFill_UsesSolidColorsAndBonfireFlickerPreservesHue),
        new(nameof(TerrainBorders_UseTilePaletteAndEntitiesStayBlack), TerrainBorders_UseTilePaletteAndEntitiesStayBlack),
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

    private static void ExtractTiles_TexturedTilesUseLighterMapTint()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        grid.SetTile(10, 10, TileType.Floor);
        SpritePacket[] sprites = new SpritePacket[grid.Width * grid.Height * 2];
        int extracted = SpriteExtraction.ExtractTiles(grid, camera, new TestTileTextures(), new Random(7), sprites);
        TestAssert.True(extracted > 1 && sprites[1].Texture.Value == 0 && sprites[1].Color == TileVisual.Color(TileType.Floor), "map tiles use solid natural floor colors");
    }

    private static void ExtractTiles_FallbackColorsAreLighter()
    {
        Vector4 originalFloor = new(0.36f, 0.66f, 0.29f, 1f);
        Vector4 lighterFloor = TileVisual.Color(TileType.Floor);
        TestAssert.True(lighterFloor.X > originalFloor.X && lighterFloor.Y > originalFloor.Y && lighterFloor.Z > originalFloor.Z, "fallback floor color is lighter");
    }

    private static void MapPalette_UsesNaturalGrassWaterAndFireColors()
    {
        Vector4 grass = TileVisual.Color(TileType.Floor);
        Vector4 water = TileVisual.Color(TileType.Water);
        Vector4 fire = TileVisual.Color(TileType.Bonfire);
        TestAssert.True(grass.Y > grass.X && grass.X > grass.Z, "grass uses a natural olive-green palette");
        TestAssert.True(water.Z > water.Y && water.Y > water.X, "water uses a blue-teal palette");
        TestAssert.True(fire.X >= 1f && fire.Y > fire.Z && fire.Z < 0.2f, "fire uses a warm yellow-orange palette");
        TestAssert.True(TileVisual.TextureTint(TileType.Water).X < TileVisual.TextureTint(TileType.Water).Y, "water texture tint shifts toward teal");
        TestAssert.True(TileVisual.TextureTint(TileType.Bonfire).Z < TileVisual.TextureTint(TileType.Bonfire).X, "fire texture tint warms the source texture");
        Vector4 fireBorder = TileVisual.BorderColor(TileType.Bonfire);
        TestAssert.True(fireBorder.X > fireBorder.Y && fireBorder.Y > fireBorder.Z && fireBorder.W >= 0.35f && fireBorder.W <= 0.45f, "fire border uses a warm translucent red-orange palette");
    }

    private static void TerrainFill_UsesSolidColorsAndBonfireFlickerPreservesHue()
    {
        TerrainSurface grid = OpenGrid();
        grid.SetTile(10, 10, TileType.Bonfire);
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] first = new SpritePacket[grid.Width * grid.Height * 2];
        SpritePacket[] second = new SpritePacket[grid.Width * grid.Height * 2];
        SpriteExtraction.ExtractTiles(grid, camera, new TestTileTextures(), new Random(1), first);
        SpriteExtraction.ExtractTiles(grid, camera, new TestTileTextures(), new Random(2), second);
        int firstFill = FindFireFill(first);
        int secondFill = FindFireFill(second);
        Vector4 baseColor = TileVisual.Color(TileType.Bonfire);
        TestAssert.True(first[firstFill].Texture.Value == 0 && second[secondFill].Texture.Value == 0, "bonfire terrain uses solid colors");
        TestAssert.True(first[firstFill].Color.X / baseColor.X == first[firstFill].Color.Y / baseColor.Y, "bonfire flicker preserves hue");
    }

    private static int FindFireFill(SpritePacket[] sprites)
    {
        for (int i = 1; i < sprites.Length; i += 2)
            if (sprites[i].Color.X > sprites[i].Color.Y && sprites[i].Color.Y > sprites[i].Color.Z) return i;
        return 1;
    }

    private static void TerrainBorders_UseTilePaletteAndEntitiesStayBlack()
    {
        TerrainSurface grid = OpenGrid();
        grid.SetTile(0, 0, TileType.Floor);
        grid.SetTile(1, 0, TileType.Water);
        grid.SetTile(2, 0, TileType.Wall);
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        SpritePacket[] terrainSprites = new SpritePacket[grid.Width * grid.Height * 2];
        SpriteExtraction.ExtractTiles(grid, camera, null, null, terrainSprites);
        TestAssert.True(terrainSprites[0].Color == TileVisual.BorderColor(TileType.Floor), "grass terrain border uses its palette");
        TestAssert.True(TileVisual.BorderColor(TileType.Water) != TileVisual.BorderColor(TileType.Floor), "water terrain border has a distinct palette");
        TestAssert.True(TileVisual.BorderColor(TileType.Wall).X == TileVisual.BorderColor(TileType.Wall).Y, "wall terrain border remains neutral");
        TestAssert.True(TileVisual.BorderColor(TileType.Floor).W >= 0.35f && TileVisual.BorderColor(TileType.Floor).W <= 0.45f, "terrain borders are semi-transparent");

        SpritePacket[] entitySprites = new SpritePacket[4];
        EntityRenderBody body = new() { Grid = grid, Camera = camera, Sprites = entitySprites };
        Position position = new(new Vector2(10.5f, 10.5f));
        Renderable renderable = new(default, new Vector2(36, 44), Vector4.One);
        EntityRenderBody.Execute(ref body, new Entity(5, 1), ref position, ref renderable);
        TestAssert.True(entitySprites[0].Color == new Vector4(0f, 0f, 0f, 1f), "entity border remains black");
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

    private sealed class TestTileTextures : ITileTextureProvider
    {
        public TextureHandle? TryGet(string name) => name == "grass" ? new TextureHandle(1) : null;
    }
}
