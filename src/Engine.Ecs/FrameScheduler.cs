using Engine.Threading;

namespace Engine.Ecs;

public sealed class FrameScheduler
{
    private readonly JobSystem _jobs;
    private readonly List<FrameStage> _stages = new();
    private readonly List<SystemRegistration> _registrations = new();
    private readonly List<SystemRegistration> _plan = new();
    private JobHandle[] _groupHandles = Array.Empty<JobHandle>();
    private bool _dirty = true;
    private int _nextOrder;

    public FrameScheduler(JobSystem jobs)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
    }

    public IReadOnlyList<FrameStage> Stages => _stages;
    public IReadOnlyList<SystemRegistration> Plan
    {
        get { if (_dirty) BuildPlan(); return _plan; }
    }

    public FrameStage AddStage(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        FrameStage stage = new(name, _stages.Count);
        _stages.Add(stage);
        _dirty = true;
        return stage;
    }

    public ParallelGroup CreateParallelGroup(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return new ParallelGroup(name);
    }

    public SystemRegistration Register(string name, FrameStage stage, ISystem system, ParallelGroup? group = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(system);
        if (!_stages.Contains(stage)) throw new InvalidOperationException("The stage does not belong to this scheduler.");
        SystemRegistration registration = new(name, system, stage, group, _nextOrder++);
        group?.Add(registration);
        stage.Add(registration);
        _registrations.Add(registration);
        _dirty = true;
        return registration;
    }

    public void BuildPlan()
    {
        _plan.Clear();
        for (int i = 0; i < _stages.Count; i++)
            for (int j = 0; j < _stages[i].Registrations.Count; j++)
                _plan.Add(_stages[i].Registrations[j]);
        if (_groupHandles.Length < _registrations.Count) _groupHandles = new JobHandle[_registrations.Count];
        _dirty = false;
    }

    public void Run(World world, float deltaSeconds)
    {
        if (_dirty) BuildPlan();
        for (int i = 0; i < _stages.Count; i++) RunStage(_stages[i], world, deltaSeconds);
    }

    private void RunStage(FrameStage stage, World world, float deltaSeconds)
    {
        for (int i = 0; i < stage.Registrations.Count;)
        {
            SystemRegistration registration = stage.Registrations[i];
            if (registration.Group is null)
            {
                registration.System.Update(world, deltaSeconds);
                i++;
                continue;
            }

            ParallelGroup group = registration.Group;
            int groupCount = 0;
            while (i < stage.Registrations.Count && ReferenceEquals(stage.Registrations[i].Group, group))
            {
                SystemRegistration item = stage.Registrations[i++];
                _groupHandles[groupCount++] = _jobs.Schedule(() => item.System.Update(world, deltaSeconds));
            }
            for (int j = 0; j < groupCount; j++) _jobs.Complete(_groupHandles[j]);
        }
    }

    public Barrier ScheduleBarrier()
        => new(_jobs, _jobs.Schedule(static () => { }));
}
