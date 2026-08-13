using System.Numerics;
using Engine.App;

namespace Engine.Tests;

internal static class TerrainSurfaceTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(SameSeed_ProducesSameSamples), SameSeed_ProducesSameSamples),
        new(nameof(SampleHeight_InterpolatesWithinBounds), SampleHeight_InterpolatesWithinBounds),
        new(nameof(ResolveMove_StopsAtBlockedSurface), ResolveMove_StopsAtBlockedSurface),
        new(nameof(SampleSurface_ReturnsMutatedMaterial), SampleSurface_ReturnsMutatedMaterial),
    ];

    private static void SameSeed_ProducesSameSamples()
    {
        TerrainSurface first = new(32, 32, seed: 7);
        TerrainSurface second = new(32, 32, seed: 7);
        TestAssert.True(first.SampleHeight(new(4.25f, 9.75f)) == second.SampleHeight(new(4.25f, 9.75f)), "same seed produces same heightfield");
        TestAssert.True(first.SampleSurface(new(12, 18)) == second.SampleSurface(new(12, 18)), "same seed produces same surfaces");
    }

    private static void SampleHeight_InterpolatesWithinBounds()
    {
        TerrainSurface terrain = new(16, 16, seed: 9);
        float height = terrain.SampleHeight(new(4.5f, 6.5f));
        TestAssert.True(height >= 0f && height <= 0.75f, "height sample stays within generated range");
        TestAssert.True(terrain.SampleHeight(new(-10, -10)) >= 0f, "height clamps at lower bounds");
    }

    private static void ResolveMove_StopsAtBlockedSurface()
    {
        TerrainSurface terrain = new(8, 8, seed: 1);
        terrain.SetTile(3, 3, TileType.Wall);
        Vector2 result = terrain.ResolveMove(new(2.5f, 3.5f), new(3.5f, 3.5f), 0.2f);
        TestAssert.True(result.X == 2.5f, "blocked terrain prevents movement");
    }

    private static void SampleSurface_ReturnsMutatedMaterial()
    {
        TerrainSurface terrain = new(8, 8, seed: 1);
        terrain.SetTile(2, 5, TileType.Water);
        TestAssert.True(terrain.SampleSurface(new(2.5f, 5.5f)) == TileType.Water, "terrain preserves surface material");
    }
}
