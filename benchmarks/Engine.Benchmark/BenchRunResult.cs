namespace Engine.Benchmark;

internal sealed record BenchRunResult(
    int SchemaVersion,
    string Machine,
    string Commit,
    DateTime TimestampUtc,
    List<BenchmarkResult> Benchmarks);
