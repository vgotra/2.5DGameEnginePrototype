namespace Engine.Ecs;

public sealed class SystemRegistration
{
    internal SystemRegistration(string name, ISystem system, FrameStage stage, ParallelGroup? group, int order)
    {
        Name = name;
        System = system;
        Stage = stage;
        Group = group;
        Order = order;
    }

    public string Name { get; }
    public ISystem System { get; }
    public FrameStage Stage { get; }
    public ParallelGroup? Group { get; }
    public int Order { get; }
}
