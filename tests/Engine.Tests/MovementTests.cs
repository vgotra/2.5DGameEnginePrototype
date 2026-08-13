using System.Numerics;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class MovementTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Move_FreeMovementAdvancesBothAxes), Move_FreeMovementAdvancesBothAxes),
        new(nameof(MoveVelocity_AppliesSpeedOnce), MoveVelocity_AppliesSpeedOnce),
        new(nameof(Move_DiagonalInputIsNormalized), Move_DiagonalInputIsNormalized),
    ];

    private static void Move_FreeMovementAdvancesBothAxes()
    {
        TileMap map = new();
        Vector2 moved = MovementSystem.Move(map, new Vector2(2.5f, 2.5f), new Vector2(1, 1), 1, 0.2f, 1);
        TestAssert.True(moved.X > 2.5f && moved.Y > 2.5f, "free movement advances both axes");
    }

    private static void MoveVelocity_AppliesSpeedOnce()
    {
        TileMap map = new();
        Vector2 moved = MovementSystem.MoveVelocity(map, new Vector2(2.5f, 2.5f), new Vector2(7, 0), 0.2f, 1f / 60f);
        TestAssert.True(MathF.Abs(moved.X - (2.5f + 7f / 60f)) < 0.001f, "velocity integration applies world speed once");
    }

    private static void Move_DiagonalInputIsNormalized()
    {
        TileMap map = new();
        Vector2 moved = MovementSystem.Move(map, new Vector2(2.5f, 2.5f), new Vector2(1, 1), 7, 0.2f, 1f / 60f);
        float distance = Vector2.Distance(moved, new Vector2(2.5f, 2.5f));
        TestAssert.True(MathF.Abs(distance - 7f / 60f) < 0.001f, "diagonal input keeps the configured speed");
    }
}
