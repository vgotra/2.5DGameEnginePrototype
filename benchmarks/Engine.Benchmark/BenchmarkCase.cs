namespace Engine.Benchmark;

internal readonly record struct BenchmarkCase(string Name, int Iterations, Action Operation);
