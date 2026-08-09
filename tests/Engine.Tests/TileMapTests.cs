using System.Numerics;
using Engine.App;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class TileMapTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(IsInside_TrueWithinBounds), IsInside_TrueWithinBounds),
        new(nameof(IsInside_FalseOutsideBounds), IsInside_FalseOutsideBounds),
        new(nameof(IsWalkable_OpenMapIsWalkable), IsWalkable_OpenMapIsWalkable),
        new(nameof(CanOccupy_OpenCellIsOccupiable), CanOccupy_OpenCellIsOccupiable),
        new(nameof(TryMove_SlidesAroundBlockedCell), TryMove_SlidesAroundBlockedCell),
        new(nameof(TryMove_BlockedCellStopsMovement), TryMove_BlockedCellStopsMovement),
    ];

    private static void IsInside_TrueWithinBounds()
    {
        TileMap map = new();
        TestAssert.True(map.IsInside(1, 1), "in-bounds cell is reported inside");
    }

    private static void IsInside_FalseOutsideBounds()
    {
        TileMap map = new();
        TestAssert.True(!map.IsInside(-1, 1), "out-of-bounds cell is reported outside");
    }

    private static void IsWalkable_OpenMapIsWalkable()
    {
        TileMap map = new();
        TestAssert.True(map.IsWalkable(0, 0), "open cell is walkable");
    }

    private static void CanOccupy_OpenCellIsOccupiable()
    {
        TileMap map = new();
        TestAssert.True(map.CanOccupy(new Vector2(10.5f, 10.5f), 0.2f), "open cell is occupiable");
    }

    private static void TryMove_SlidesAroundBlockedCell()
    {
        TileMap map = new();
        map.SetTile(3, 3, TileType.Blocked);
        Vector2 slid = map.TryMove(new(3.5f, 2.5f), new(4.0f, 3.0f), 0.2f);
        TestAssert.True(slid.X == 4.0f && slid.Y == 2.5f, "movement slides horizontally around a blocked cell");
    }

    private static void TryMove_BlockedCellStopsMovement()
    {
        TileMap map = new();
        map.SetTile(3, 3, TileType.Blocked);
        Vector2 stopped = map.TryMove(new(3.4f, 2.4f), new(3.4f, 3.0f), 0.2f);
        TestAssert.True(stopped == new Vector2(3.4f, 2.4f), "blocked cell stops movement");
    }
}
