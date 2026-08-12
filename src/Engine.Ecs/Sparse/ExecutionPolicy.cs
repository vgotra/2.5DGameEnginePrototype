namespace Engine.Ecs.Sparse;

public enum ExecutionPolicy
{
    Serial,
    Adaptive,
    Parallel,
    Background
}

public readonly record struct SystemPolicyMetadata(
    string Name,
    ExecutionPolicy Policy,
    int AdaptiveThreshold,
    bool FixedStep,
    bool Deterministic,
    bool BackgroundOnly);

public readonly record struct SystemDiagnostic(
    string Name,
    ExecutionPolicy Policy,
    bool ParallelSelected,
    int ItemCount,
    long ElapsedTicks,
    long AllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);
