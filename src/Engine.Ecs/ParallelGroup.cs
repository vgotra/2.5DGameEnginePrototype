namespace Engine.Ecs;

public sealed class ParallelGroup
{
    private readonly List<SystemRegistration> _registrations = new();

    internal ParallelGroup(string name) { Name = name; }

    public string Name { get; }
    internal List<SystemRegistration> Registrations => _registrations;

    internal void Add(SystemRegistration registration)
    {
        for (int i = 0; i < _registrations.Count; i++)
            if (Conflicts(registration.System.Access, _registrations[i].System.Access))
                throw new InvalidOperationException($"Systems '{registration.Name}' and '{_registrations[i].Name}' conflict in parallel group '{Name}'.");
        _registrations.Add(registration);
    }

    private static bool Conflicts(ComponentAccess a, ComponentAccess b)
        => Overlaps(a.WriteTypes, b.ReadTypes) || Overlaps(a.WriteTypes, b.WriteTypes) || Overlaps(a.ReadTypes, b.WriteTypes);

    private static bool Overlaps(ComponentTypeId[] left, ComponentTypeId[] right)
    {
        for (int i = 0; i < left.Length; i++)
            for (int j = 0; j < right.Length; j++)
                if (left[i] == right[j]) return true;
        return false;
    }
}
