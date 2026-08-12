using Engine.Threading;

namespace Engine.Ecs.Sparse;

public sealed class FrameScheduler
{
    private readonly JobSystem _jobs;
    private readonly List<ISystem> _systems = new();

    public FrameScheduler(JobSystem jobs) => _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public void Register(ISystem system) => _systems.Add(system ?? throw new ArgumentNullException(nameof(system)));

    public void Run(World world, float deltaSeconds)
    {
        for (int i = 0; i < _systems.Count; i++) _systems[i].Update(world, deltaSeconds);
    }
}
