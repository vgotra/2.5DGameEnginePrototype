using Engine.Ecs.Sparse;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class PolicyBenchmarks
{
    private static readonly JobSystem Jobs = new(1);

    public static void Dispose() => Jobs.Dispose();

    public static BenchmarkCase[] Create()
    {
        return
        [
            CreateCase("Policy_Serial", ExecutionPolicy.Serial, 0),
            CreateCase("Policy_Adaptive", ExecutionPolicy.Adaptive, 64),
            CreateCase("Policy_Parallel", ExecutionPolicy.Parallel, 0)
        ];
    }

    private static BenchmarkCase CreateCase(string name, ExecutionPolicy policy, int threshold)
    {
        FrameScheduler scheduler = new(Jobs);
        World world = new();
        world.Create();
        scheduler.Register(new NoOpSystem(), new(name, policy, threshold, true, true, false));
        scheduler.DiagnosticsEnabled = false;
        return new BenchmarkCase(name, 10_000, () => scheduler.Run(world, 1f / 60f));
    }

    private sealed class NoOpSystem : ISystem
    {
        public void Update(World world, float deltaSeconds) { }
    }
}
