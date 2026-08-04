using System.Numerics;
using Engine.Mathematics;

namespace Engine.Tests;

internal static class IsometricMathTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(WorldToScreen_ScreenToWorld_RoundTrips), WorldToScreen_ScreenToWorld_RoundTrips),
    ];

    private static void WorldToScreen_ScreenToWorld_RoundTrips()
    {
        Vector2 original = new(7, 2);
        Vector2 projected = IsometricMath.WorldToScreen(original, 64, 32);
        Vector2 restored = IsometricMath.ScreenToWorld(projected, 64, 32);
        TestAssert.True(MathF.Abs(restored.X - original.X) < 0.001f && MathF.Abs(restored.Y - original.Y) < 0.001f, "isometric conversion round-trips");
    }
}
