using Engine.Threading;
using System.Diagnostics;

namespace Engine.Ecs.Sparse;

public sealed class FrameScheduler
{
    private readonly JobSystem _jobs;
    private readonly List<ISystem> _systems = new();
    private readonly List<SystemPolicyMetadata> _metadata = new();
    private SystemDiagnostic[] _diagnostics = Array.Empty<SystemDiagnostic>();

    public bool DiagnosticsEnabled { get; set; }
    public ReadOnlySpan<SystemDiagnostic> Diagnostics => _diagnostics;

    public FrameScheduler(JobSystem jobs) => _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public void Register(ISystem system) => Register(system, new(system.GetType().Name, ExecutionPolicy.Serial, 0, true, true, false));

    public void Register(ISystem system, SystemPolicyMetadata metadata)
    {
        _systems.Add(system ?? throw new ArgumentNullException(nameof(system)));
        _metadata.Add(metadata);
        if (_diagnostics.Length < _systems.Count) _diagnostics = new SystemDiagnostic[_systems.Count];
    }

    public void Run(World world, float deltaSeconds)
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            ISystem system = _systems[i];
            SystemPolicyMetadata metadata = _metadata[i];
            int itemCount = world.EntityCount;
            bool parallel = metadata.Policy == ExecutionPolicy.Parallel || metadata.Policy == ExecutionPolicy.Adaptive && itemCount >= metadata.AdaptiveThreshold;
            if (metadata.Policy == ExecutionPolicy.Background) continue;
            if (!DiagnosticsEnabled)
            {
                system.Update(world, deltaSeconds);
                continue;
            }
            long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);
            long start = Stopwatch.GetTimestamp();
            system.Update(world, deltaSeconds);
            _diagnostics[i] = new(metadata.Name, metadata.Policy, parallel, itemCount, Stopwatch.GetTimestamp() - start,
                GC.GetAllocatedBytesForCurrentThread() - beforeBytes, GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2);
        }
    }
}
