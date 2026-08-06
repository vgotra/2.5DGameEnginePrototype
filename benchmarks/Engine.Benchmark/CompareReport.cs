namespace Engine.Benchmark;

internal sealed record CompareReport(bool SameMachine, int Failures, List<CompareEntry> Entries);
