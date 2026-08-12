namespace Engine.Benchmark.Benchmarks;

internal static class BenchmarkCatalog
{
    public static BenchmarkCase[] Create(int? iterationsOverride)
    {
        BenchmarkCase[] cases =
        [
            .. ExtractionBenchmarks.Create(),
            .. CollisionBenchmarks.Create(),
            .. SparseEcsBenchmarks.Create(),
            .. SparseQueryBenchmarks.Create(),
            .. FrameSchedulerBenchmarks.Create(),
            .. BufferBenchmarks.Create(),
            .. MathBenchmarks.Create(),
            .. JobsBenchmarks.Create(),
            .. ArpgBenchmarks.Create(),
            .. PolicyBenchmarks.Create(),
        ];
        if (iterationsOverride is not int count) return cases;
        BenchmarkCase[] overridden = new BenchmarkCase[cases.Length];
        for (int i = 0; i < cases.Length; i++) overridden[i] = cases[i] with { Iterations = count };
        return overridden;
    }
}
