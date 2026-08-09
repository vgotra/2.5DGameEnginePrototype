using Engine.Core;
using Engine.Ecs;

namespace Engine.Benchmark.Benchmarks;

internal static class EcsBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        World world = new();
        const int PoolSize = 256;
        EntityId[] pool = new EntityId[PoolSize];
        for (int i = 0; i < PoolSize; i++) pool[i] = world.Create();
        world.AddComponent(pool[0], 1);
        int cursor = 0;
        bool hit = false;

        return
        [
            new BenchmarkCase("Ecs_AddRemoveComponent", 200_000,
                () =>
                {
                    EntityId entity = pool[cursor++ & (PoolSize - 1)];
                    world.AddComponent(entity, 1);
                    world.RemoveComponent<int>(entity);
                }),
            new BenchmarkCase("Ecs_TryGetHit", 200_000,
                () => { hit ^= world.TryGetComponent(pool[0], out int _); }),
            new BenchmarkCase("Ecs_RemoveMiss", 200_000,
                () => { world.RemoveComponent<int>(new EntityId(999_999, 1)); }),
        ];
    }
}
