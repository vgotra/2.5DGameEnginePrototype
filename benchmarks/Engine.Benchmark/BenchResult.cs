namespace Engine.Benchmark;

/// <summary>One full benchmark run: environment provenance plus per-benchmark results.</summary>
internal sealed record BenchRunResult(
    int SchemaVersion,
    string Machine,
    string Commit,
    DateTime TimestampUtc,
    List<BenchmarkResult> Benchmarks);

/// <summary>Measured result for a single benchmark.</summary>
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
