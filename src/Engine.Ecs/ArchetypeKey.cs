namespace Engine.Ecs;

internal readonly struct ArchetypeKey : IEquatable<ArchetypeKey>
{
    public readonly ComponentTypeId[] Types;
    private readonly int _hash;

    public ArchetypeKey(ComponentTypeId[] types)
    {
        Types = types;
        int hash = 17;
        for (int i = 0; i < types.Length; i++) hash = hash * 31 + types[i].Value;
        _hash = hash;
    }

    public bool Equals(ArchetypeKey other)
    {
        ComponentTypeId[] a = Types;
        ComponentTypeId[] b = other.Types;
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is ArchetypeKey other && Equals(other);

    public override int GetHashCode() => _hash;
}
