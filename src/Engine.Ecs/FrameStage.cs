namespace Engine.Ecs;

public sealed class FrameStage
{
    private readonly List<SystemRegistration> _registrations = new();

    internal FrameStage(string name, int order) { Name = name; Order = order; }

    public string Name { get; }
    public int Order { get; }
    internal List<SystemRegistration> Registrations => _registrations;

    internal void Add(SystemRegistration registration) => _registrations.Add(registration);
}
