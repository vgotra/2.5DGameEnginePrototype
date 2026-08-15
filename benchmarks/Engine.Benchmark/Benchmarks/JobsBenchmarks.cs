using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class JobsBenchmarks
{
    private static JobSystem? _jobs;
    private static int[] _sink = [];
    private static int _counter;
    private static readonly Action _work = () => Interlocked.Increment(ref _counter);
    private static readonly Action<int, int> _body = Work;
    private static readonly Action<int, int> _tinyBody = TinyWork;
    private static int[] _tinySink = new int[64];

    public static BenchmarkCase[] Create()
    {
        _jobs ??= new JobSystem();
        _sink = new int[1 << 20];
        return
        [
            new BenchmarkCase("Jobs_RunWait_64", 20_000, RunRunWait),
            new BenchmarkCase("Jobs_RunWait_16", 20_000, RunRunWaitBatch),
            new BenchmarkCase("Jobs_ParallelFor_1M", 2_000, RunParallelFor),
            new BenchmarkCase("Jobs_ParallelFor_TinyChunks", 20_000, RunTinyParallelFor),
            new BenchmarkCase("Jobs_SlotReuse", 20_000, RunSlotReuse),
        ];
    }

    private static void Work(int lo, int hi)
    {
        for (int i = lo; i < hi; i++) _sink[i] = i;
    }

    private static void RunRunWait()
    {
        for (int i = 0; i < 64; i++)
        {
            JobHandle handle = _jobs!.Run(_work);
            _jobs.Wait(handle);
        }
    }

    private static void RunParallelFor()
    {
        JobHandle barrier = _jobs!.ParallelFor(_sink.Length, 8192, _body);
        _jobs.Wait(barrier);
    }

    private static void RunRunWaitBatch()
    {
        for (int i = 0; i < 16; i++)
        {
            JobHandle handle = _jobs!.Run(_work);
            _jobs.Wait(handle);
        }
    }

    private static void RunTinyParallelFor()
    {
        JobHandle barrier = _jobs!.ParallelFor(_tinySink.Length, 1, _tinyBody);
        _jobs.Wait(barrier);
    }

    private static void RunSlotReuse()
    {
        JobHandle handle = _jobs!.Run(_work);
        _jobs.Wait(handle);
    }

    private static void TinyWork(int lo, int hi)
    {
        for (int i = lo; i < hi; i++) _tinySink[i] = i;
    }
}
