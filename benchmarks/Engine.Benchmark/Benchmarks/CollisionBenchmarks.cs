using System.Numerics;
using Engine.App;
using IsometricSandbox.Game.Gameplay.Systems;

namespace Engine.Benchmark.Benchmarks;

internal static class CollisionBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        TerrainSurface open = new(20, 20);
        Vector2 position = new(10.5f, 10.5f);
        Vector2 input = new(1f, 0.7f);
        float dt = 1f / 60f;
        Vector2 sink = default;

        TerrainSurface blocked = new(20, 20);
        blocked.SetTile(10, 10, TileType.Blocked);
        Vector2 blockedPos = new(9.5f, 9.5f);
        Vector2 blockedSink = default;

        return
        [
            new BenchmarkCase("Terrain_Collision_OpenMovement", 50_000,
                () => { sink += open.TryMove(position, position + input * 0.5f, 0.2f); }),
            new BenchmarkCase("Terrain_Movement_Resolution", 50_000,
                () => { sink += MovementSystem.Move(open, position, input, 4f, 0.2f, dt); }),
            new BenchmarkCase("Terrain_Collision_BlockedMovement", 50_000,
                () => { blockedSink += blocked.TryMove(blockedPos, blockedPos + new Vector2(1f, 0f), 0.2f); }),
        ];
    }
}
