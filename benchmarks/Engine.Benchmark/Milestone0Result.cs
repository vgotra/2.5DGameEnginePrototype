namespace Engine.Benchmark;

internal sealed record Milestone0Result(
    int EntityCount,
    string Mode,
    int Iterations,
    double SimulationMs,
    double FrameMs,
    double SchedulerOverheadUs,
    double AllocBytesPerFrame,
    int Gen0,
    int Gen1,
    int Gen2,
    int JobsPerFrame,
    int WorkerCount,
    int ActiveWorkers,
    double WorkerUtilization,
    long Checksum);
