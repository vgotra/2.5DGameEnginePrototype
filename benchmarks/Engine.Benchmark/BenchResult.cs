namespace Engine.Benchmark;

internal sealed record BenchRunResult(
    int SchemaVersion,
    string Machine,
    string Commit,
    DateTime TimestampUtc,
    List<BenchmarkResult> Benchmarks);

internal sealed record BenchmarkResult(
    string Name,
    int Iterations,
    double MedianNsPerOp,
    double MinNsPerOp,
    double MaxNsPerOp,
    double AllocBytesPerOp,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);
