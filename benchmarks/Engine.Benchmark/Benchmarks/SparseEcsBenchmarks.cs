using SparseEntity = Engine.Ecs.Sparse.Entity;
using SparseWorld = Engine.Ecs.Sparse.World;

namespace Engine.Benchmark.Benchmarks;

internal static class SparseEcsBenchmarks
{
    public static BenchmarkCase[] Create()
    {
        SparseWorld world = new();
        SparseEntity[] pool = new SparseEntity[256];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = world.Create();
            world.Add(pool[i], 1);
        }
        int cursor = 0;
        bool hit = false;
        return
        [
            new BenchmarkCase("SparseEcs_AddRemoveComponent", 200_000, () =>
            {
                SparseEntity entity = pool[cursor++ & (pool.Length - 1)];
                world.Add(entity, 1);
                world.Remove<int>(entity);
            }),
            new BenchmarkCase("SparseEcs_TryGetHit", 200_000, () =>
            {
                hit ^= world.TryGet(pool[0], out int _);
            }),
            new BenchmarkCase("SparseEcs_RemoveMiss", 200_000, () =>
            {
                world.Remove<int>(new SparseEntity(999_999, 1));
            }),
            new BenchmarkCase("SparseEcs_CreateDestroyRecycle", 200_000, () =>
            {
                SparseEntity entity = world.Create();
                world.Destroy(entity);
            }),
        ];
    }
}
