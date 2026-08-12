using IsometricSandbox.Game;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class ArpgBenchmarks
{
    private static readonly JobSystem Jobs = new();

    public static void Dispose() => Jobs.Dispose();

    public static BenchmarkCase[] Create()
    {
        return
        [
            CreateCase("ArpgGameplay_Serial", ArpgExecutionMode.Serial),
            CreateCase("ArpgGameplay_AdaptiveParallel", ArpgExecutionMode.AdaptiveParallel),
            CreateCase("ArpgGameplay_ForcedParallel", ArpgExecutionMode.ForcedParallel)
        ];
    }

    private static BenchmarkCase CreateCase(string name, ArpgExecutionMode mode)
    {
        ArpgWorkload workload = new(1337, Jobs);
        return new BenchmarkCase(name, 200, () => workload.Tick(mode));
    }
}
