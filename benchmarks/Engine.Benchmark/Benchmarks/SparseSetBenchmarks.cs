using Engine.Core;
using Engine.Ecs;

namespace Engine.Benchmark.Benchmarks;

internal static class SparseSetBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        World world = new();
        SparseSet<int> storage = world.Storage<int>();
        const int PoolSize = 256;
        EntityId[] pool = new EntityId[PoolSize];
        for (int i = 0; i < PoolSize; i++) pool[i] = world.Create();
        int cursor = 0;
        bool hit = false;

        return
        [
            new BenchmarkCase("SparseSet_AddRemove", 200_000,
                () =>
                {
                    EntityId entity = pool[cursor++ & (PoolSize - 1)];
                    storage.Add(entity, 1);
                    storage.Remove(entity);
                }),
            new BenchmarkCase("SparseSet_TryGetHit", 200_000,
                () => { hit ^= storage.TryGet(pool[0], out _); }),
            new BenchmarkCase("SparseSet_RemoveMiss", 200_000,
                () => { storage.Remove(new EntityId(999_999, 1)); }),
        ];
    }
}
