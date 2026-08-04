using System.Diagnostics;

namespace Engine.Benchmark;

internal readonly record struct BenchmarkCase(string Name, int Iterations, Action Operation);

/// <summary>
/// Measurement core: warmup runs happen outside measurement (JIT/tiered compilation and first-time
/// growth settle there), then a single-threaded allocation pass measures exact per-thread bytes and
/// GC collection deltas, and a multi-trial timing pass reports median/min/max ns per operation.
/// </summary>
internal static class BenchRunner
{
    private const int TimingTrials = 7;

    public static BenchmarkResult Run(BenchmarkCase benchmark)
    {
        Action operation = benchmark.Operation;
        int iterations = benchmark.Iterations;
        int warmup = Math.Max(5, Math.Min(iterations / 10, 1000));
        for (int i = 0; i < warmup; i++) operation();

        long startAlloc = GC.GetAllocatedBytesForCurrentThread();
        long startGen0 = GC.CollectionCount(0);
        long startGen1 = GC.CollectionCount(1);
        long startGen2 = GC.CollectionCount(2);
        for (int i = 0; i < iterations; i++) operation();
        double allocBytesPerOp = (double)(GC.GetAllocatedBytesForCurrentThread() - startAlloc) / iterations;
        int gen0 = (int)(GC.CollectionCount(0) - startGen0);
        int gen1 = (int)(GC.CollectionCount(1) - startGen1);
        int gen2 = (int)(GC.CollectionCount(2) - startGen2);

        double[] perOp = new double[TimingTrials];
        for (int trial = 0; trial < TimingTrials; trial++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; i++) operation();
            long elapsed = Stopwatch.GetTimestamp() - start;
            perOp[trial] = elapsed * 1e9 / (iterations * (double)Stopwatch.Frequency);
        }
        Array.Sort(perOp);

        return new BenchmarkResult(
            benchmark.Name,
            iterations,
            perOp[TimingTrials / 2],
            perOp[0],
            perOp[^1],
            allocBytesPerOp,
            gen0,
            gen1,
            gen2);
    }
}
