using Engine.Ecs.Sparse;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class FrameSchedulerBenchmarks
{
    private static JobSystem? _jobs;
    private static FrameScheduler? _scheduler;
    private static readonly World World = new();

    public static BenchmarkCase[] Create()
    {
        _jobs ??= new JobSystem(1);
        _scheduler ??= CreateScheduler();
        return
        [
            new BenchmarkCase("SparseFrameScheduler_RegisterRun", 50_000, RunScheduler),
        ];
    }

    private static FrameScheduler CreateScheduler()
    {
        FrameScheduler scheduler = new(_jobs!);
        scheduler.Register(new EmptySystem());
        return scheduler;
    }

    private static void RunScheduler() => _scheduler!.Run(World, 0.016f);

    private sealed class EmptySystem : ISystem
    {
        public void Update(World world, float deltaSeconds) { }
    }
}
