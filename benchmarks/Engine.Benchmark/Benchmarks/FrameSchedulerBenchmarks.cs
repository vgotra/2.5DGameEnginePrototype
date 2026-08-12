using Engine.Ecs;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class FrameSchedulerBenchmarks
{
    private static JobSystem? _jobs;
    private static FrameScheduler? _sequential;
    private static FrameScheduler? _parallel;
    private static readonly World World = new();
    private static readonly ISystem NoOp = new EmptySystem();

    public static BenchmarkCase[] Create()
    {
        _jobs ??= new JobSystem(2);
        _sequential ??= CreateScheduler(false);
        _parallel ??= CreateScheduler(true);
        return
        [
            new BenchmarkCase("FrameScheduler_PlanBuild", 10_000, RunPlanBuild),
            new BenchmarkCase("FrameScheduler_Sequential", 100_000, RunSequential),
            new BenchmarkCase("FrameScheduler_ParallelGroup", 10_000, RunParallel),
            new BenchmarkCase("FrameScheduler_Barrier", 100_000, RunBarrier),
        ];
    }

    private static FrameScheduler CreateScheduler(bool parallel)
    {
        FrameScheduler scheduler = new(_jobs!);
        FrameStage stage = scheduler.AddStage("update");
        ParallelGroup? group = parallel ? scheduler.CreateParallelGroup("independent") : null;
        scheduler.Register("one", stage, NoOp, group);
        scheduler.Register("two", stage, NoOp, group);
        scheduler.BuildPlan();
        return scheduler;
    }

    private static void RunPlanBuild()
    {
        FrameScheduler scheduler = new(_jobs!);
        FrameStage stage = scheduler.AddStage("update");
        scheduler.Register("one", stage, NoOp);
        scheduler.BuildPlan();
    }

    private static void RunSequential() => _sequential!.Run(World, 0.016f);
    private static void RunParallel() => _parallel!.Run(World, 0.016f);

    private static void RunBarrier() => _parallel!.ScheduleBarrier().Complete();

    private sealed class EmptySystem : ISystem
    {
        public ComponentAccess Access => ComponentAccess.Read<int>();
        public void Update(World world, float deltaSeconds) { }
    }
}
