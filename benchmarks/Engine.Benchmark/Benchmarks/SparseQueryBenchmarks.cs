using SparseEntity = Engine.Ecs.Sparse.Entity;
using SparseWorld = Engine.Ecs.Sparse.World;
using Engine.Ecs.Sparse;

namespace Engine.Benchmark.Benchmarks;

internal static class SparseQueryBenchmarks
{
    private static readonly int[] Counts = [100, 500, 1_000, 5_000, 100_000];

    public static BenchmarkCase[] Create()
    {
        List<BenchmarkCase> benchmarks = new();
        foreach (int count in Counts) AddCases(benchmarks, count);
        return benchmarks.ToArray();
    }

    private static void AddCases(List<BenchmarkCase> benchmarks, int count)
    {
        SparseWorld world = new();
        for (int i = 0; i < count; i++)
        {
            SparseEntity entity = world.Create();
            world.Add(entity, new Position(i));
            if ((i & 1) == 0) world.Add(entity, new Velocity(i));
            if ((i & 3) == 0) world.Add(entity, new Marker(i));
        }

        Query<Position> query1 = world.Query<Position>();
        Query<Position, Velocity> query2 = world.Query<Position, Velocity>();
        Query<Position, Velocity, Marker> query3 = world.Query<Position, Velocity, Marker>();

        benchmarks.Add(new BenchmarkCase($"SparseQuery_{count}_1", 1_000, () =>
        {
            SumPosition action = new();
            query1.ForEach(ref action);
        }));
        benchmarks.Add(new BenchmarkCase($"SparseQuery_{count}_2", 1_000, () =>
        {
            SumPair action = new();
            query2.ForEach(ref action);
        }));
        benchmarks.Add(new BenchmarkCase($"SparseQuery_{count}_3", 1_000, () =>
        {
            SumTriple action = new();
            query3.ForEach(ref action);
        }));
    }

    private readonly record struct Position(int Value);
    private readonly record struct Velocity(int Value);
    private readonly record struct Marker(int Value);

    private struct SumPosition : IQueryAction<Position, SumPosition>
    {
        public int Sum;
        public static void Execute(ref SumPosition action, SparseEntity entity, ref Position component) => action.Sum += component.Value;
    }

    private struct SumPair : IQueryAction<Position, Velocity, SumPair>
    {
        public int Sum;
        public static void Execute(ref SumPair action, SparseEntity entity, ref Position first, ref Velocity second) => action.Sum += first.Value + second.Value;
    }

    private struct SumTriple : IQueryAction<Position, Velocity, Marker, SumTriple>
    {
        public int Sum;
        public static void Execute(ref SumTriple action, SparseEntity entity, ref Position first, ref Velocity second, ref Marker third) => action.Sum += first.Value + second.Value + third.Value;
    }
}
