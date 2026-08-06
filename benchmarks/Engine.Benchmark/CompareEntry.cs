namespace Engine.Benchmark;

internal sealed record CompareEntry(
    string Name,
    string Verdict,
    double BaselineNs,
    double CurrentNs,
    double TimeDeltaPct,
    double CurrentAllocBytes);
