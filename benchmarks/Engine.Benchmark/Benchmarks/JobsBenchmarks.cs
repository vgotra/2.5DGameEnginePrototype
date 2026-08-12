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
            new BenchmarkCase("Jobs_RunWait_64", 20_000, RunRunWait),
            new BenchmarkCase("Jobs_ParallelFor_1M", 2_000, RunParallelFor),
        ];
    }

    private static void Work(int lo, int hi)
    {
        for (int i = lo; i < hi; i++) _sink[i] = i;
    }

    private static void RunRunWait()
    {
        JobHandle last = JobHandle.None;
        for (int i = 0; i < 64; i++) last = _jobs!.Run(_work);
        _jobs!.Wait(last);
    }

    private static void RunParallelFor()
    {
        JobHandle barrier = _jobs!.ParallelFor(_sink.Length, 8192, _body);
        _jobs.Wait(barrier);
    }
}
