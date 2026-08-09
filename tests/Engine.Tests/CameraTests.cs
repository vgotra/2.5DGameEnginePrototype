using System.Numerics;
using Engine.App;

namespace Engine.Tests;

internal static class CameraTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Follow_IsoCameraCentersViewport), Follow_IsoCameraCentersViewport),
        new(nameof(Follow_IsoMapCenteredInFullscreen), Follow_IsoMapCenteredInFullscreen),
        new(nameof(Follow_FlatCameraCentersViewport), Follow_FlatCameraCentersViewport),
        new(nameof(Follow_FlatCameraMapsTileRightward), Follow_FlatCameraMapsTileRightward),
        new(nameof(Follow_FlatMapCenteredHorizontallyInFullscreen), Follow_FlatMapCenteredHorizontallyInFullscreen),
        new(nameof(Follow_FlatMapCenteredOnBothAxesWhenFitted), Follow_FlatMapCenteredOnBothAxesWhenFitted),
    ];

    private static TileGrid OpenGrid() => new(20, 20, 64, 32, new byte[400]);

    private static void Follow_IsoCameraCentersViewport()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid) == new Vector2(400, 300), "iso camera centers the followed point");
    }

    private static void Follow_IsoMapCenteredInFullscreen()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(1920, 1080));
        camera.Follow(new Vector2(2, 2), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid) == new Vector2(960, 540), "iso map is centered in the fullscreen viewport");
    }

    private static void Follow_FlatCameraCentersViewport()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid) == new Vector2(400, 300), "flat camera centers the followed point");
    }

    private static void Follow_FlatCameraMapsTileRightward()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(10, 10), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(11, 10), grid) == new Vector2(464, 300), "flat camera maps an adjacent tile one tile right");
    }

    private static void Follow_FlatMapCenteredHorizontallyInFullscreen()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(1920, 1080)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(2, 2), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid).X == 960, "flat map is centered horizontally in the fullscreen viewport");
    }

    private static void Follow_FlatMapCenteredOnBothAxesWhenFitted()
    {
        TileGrid grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(1280, 1280)) { Mode = GameMode.TopDown };
        camera.Follow(new Vector2(2, 2), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid) == new Vector2(640, 640), "flat map is centered on both axes when it fits the viewport");
    }
}
