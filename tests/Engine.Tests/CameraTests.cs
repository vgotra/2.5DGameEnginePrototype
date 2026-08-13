using System.Numerics;
using Engine.App;

namespace Engine.Tests;

internal static class CameraTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Follow_IsoCameraCentersViewport), Follow_IsoCameraCentersViewport),
        new(nameof(Follow_IsoMapCenteredInFullscreen), Follow_IsoMapCenteredInFullscreen),
    ];

    private static TerrainSurface OpenGrid()
    {
        TerrainSurface terrain = new(20, 20, 1f, 64f, 32f, 7);
        for (int y = 0; y < terrain.Height; y++)
            for (int x = 0; x < terrain.Width; x++)
                terrain.SetHeight(x, y, 0f);
        return terrain;
    }

    private static void Follow_IsoCameraCentersViewport()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid) == new Vector2(400, 300), "iso camera centers the followed point");
    }

    private static void Follow_IsoMapCenteredInFullscreen()
    {
        TerrainSurface grid = OpenGrid();
        IsometricCamera camera = new(new Vector2(1920, 1080));
        camera.Follow(new Vector2(10, 10), grid);
        TestAssert.True(camera.WorldToScreen(new Vector2(10, 10), grid) == new Vector2(960, 540), "iso map is centered in the fullscreen viewport");
    }

}
