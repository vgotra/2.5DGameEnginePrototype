using System.Numerics;
using IsometricSandbox.Game;

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

    private static void Follow_IsoCameraCentersViewport()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), map);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(400, 300), "iso camera centers the followed point");
    }

    private static void Follow_IsoMapCenteredInFullscreen()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(1920, 1080));
        camera.Follow(new Vector2(2, 2), map);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(960, 540), "iso map is centered in the fullscreen viewport");
    }

    private static void Follow_FlatCameraCentersViewport()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600)) { Isometric = false };
        camera.Follow(new Vector2(10, 10), map);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(400, 300), "flat camera centers the followed point");
    }

    private static void Follow_FlatCameraMapsTileRightward()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(800, 600)) { Isometric = false };
        camera.Follow(new Vector2(10, 10), map);
        TestAssert.True(camera.WorldToScreen(new Vector2(11, 10), map) == new Vector2(464, 300), "flat camera maps an adjacent tile one tile right");
    }

    private static void Follow_FlatMapCenteredHorizontallyInFullscreen()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(1920, 1080)) { Isometric = false };
        camera.Follow(new Vector2(2, 2), map);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), map).X == 960, "flat map is centered horizontally in the fullscreen viewport");
    }

    private static void Follow_FlatMapCenteredOnBothAxesWhenFitted()
    {
        TileMap map = new();
        IsometricCamera camera = new(new Vector2(1280, 1280)) { Isometric = false };
        camera.Follow(new Vector2(2, 2), map);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(640, 640), "flat map is centered on both axes when it fits the viewport");
    }
}
