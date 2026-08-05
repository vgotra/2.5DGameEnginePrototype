using System.Numerics;
using Engine.Rendering;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class RenderExtractionTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(ExtractMapSprites_IsoExtractionBounded), ExtractMapSprites_IsoExtractionBounded),
        new(nameof(ExtractMapSprites_IsoSpritesAreDiamonds), ExtractMapSprites_IsoSpritesAreDiamonds),
        new(nameof(ExtractMapSprites_FlatExtractionBounded), ExtractMapSprites_FlatExtractionBounded),
        new(nameof(ExtractMapSprites_FlatSpritesAreBoxes), ExtractMapSprites_FlatSpritesAreBoxes),
        new(nameof(ExtractMapSprites_CullsOffScreenTiles), ExtractMapSprites_CullsOffScreenTiles),
    ];

    private static void ExtractMapSprites_IsoExtractionBounded()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), map);
        SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2];
        int extracted = RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
        TestAssert.True(extracted > 0 && extracted <= map.Width * map.Height * 2, "iso map sprite extraction is bounded");
    }

    private static void ExtractMapSprites_IsoSpritesAreDiamonds()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), map);
        SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2];
        RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
        TestAssert.True(sprites[0].Shape == ShapeKind.Diamond && sprites[1].Shape == ShapeKind.Diamond, "iso sprites are diamonds");
    }

    private static void ExtractMapSprites_FlatExtractionBounded()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), map);
        SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2];
        int extracted = RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
        TestAssert.True(extracted > 0 && extracted <= map.Width * map.Height * 2, "flat map sprite extraction is bounded");
    }

    private static void ExtractMapSprites_FlatSpritesAreBoxes()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), map);
        SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2];
        RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
        TestAssert.True(sprites[0].Shape == ShapeKind.Box && sprites[1].Shape == ShapeKind.Box && sprites[1].Size == new Vector2(map.TileWidth, map.TileWidth), "flat sprites are boxes");
    }

    private static void ExtractMapSprites_CullsOffScreenTiles()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(300, 300)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), map);
        SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2];
        int extracted = RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
        TestAssert.True(extracted > 0 && extracted < map.Width * map.Height * 2, "viewport culling skips off-screen tiles");
    }
}
