using Engine.Threading;

namespace Engine.Ecs;

public sealed class Barrier
{
    private readonly JobSystem _jobs;
    private readonly JobHandle _handle;

    internal Barrier(JobSystem jobs, JobHandle handle)
    {
        _jobs = jobs;
        _handle = handle;
    }

    public bool IsComplete => _jobs.IsComplete(_handle);
    public void Complete() => _jobs.Complete(_handle);
}
