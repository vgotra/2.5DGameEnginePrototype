using System.Numerics;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class MovementTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Move_FreeMovementAdvancesBothAxes), Move_FreeMovementAdvancesBothAxes),
    ];

    private static void Move_FreeMovementAdvancesBothAxes()
    {
        TileMap map = new();
        Vector2 moved = MovementSystem.Move(map, new Vector2(2.5f, 2.5f), new Vector2(1, 1), 1, 0.2f, 1);
        TestAssert.True(moved.X > 2.5f && moved.Y > 2.5f, "free movement advances both axes");
    }
}
