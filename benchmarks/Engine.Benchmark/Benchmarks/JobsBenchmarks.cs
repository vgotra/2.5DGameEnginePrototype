using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class JobsBenchmarks
{
    private static JobSystem? _jobs;
    private static int[] _sink = [];
    private static int _counter;
    private static readonly Action _work = () => Interlocked.Increment(ref _counter);
    private static readonly Action<int, int> _body = Work;

    public static BenchmarkCase[] Create()
    {
        _jobs ??= new JobSystem();
        _sink = new int[1 << 20];
        return
        [
            new BenchmarkCase("Jobs_ScheduleComplete_64", 20_000, RunScheduleComplete),
            new BenchmarkCase("Jobs_ScheduleFor_1M", 2_000, RunScheduleFor),
        ];
    }

    private static void Work(int lo, int hi)
    {
        for (int i = lo; i < hi; i++) _sink[i] = i;
    }

    private static void RunScheduleComplete()
    {
        JobHandle last = JobHandle.None;
        for (int i = 0; i < 64; i++) last = _jobs!.Schedule(_work);
        _jobs!.Complete(last);
    }

    private static void RunScheduleFor()
    {
        JobHandle barrier = _jobs!.ScheduleFor(_sink.Length, 8192, _body);
        _jobs.Complete(barrier);
    }
}
