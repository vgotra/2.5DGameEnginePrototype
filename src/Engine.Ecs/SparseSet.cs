using Engine.Core;

namespace Engine.Ecs;

public sealed class SparseSet<T> where T : unmanaged
{
    private readonly Dictionary<uint, int> _indices = new();
    private readonly List<EntityId> _entities = new();
    private readonly List<T> _values = new();

    public int Count => _values.Count;
    public ReadOnlySpan<EntityId> Entities => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_entities);
    public ReadOnlySpan<T> Values => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_values);

    public void Add(EntityId entity, T value)
    {
        if (_indices.TryGetValue(entity.Index, out int index)) { _values[index] = value; return; }
        _indices.Add(entity.Index, _values.Count);
        _entities.Add(entity);
        _values.Add(value);
    }

    public bool TryGet(EntityId entity, out T value)
    {
        if (_indices.TryGetValue(entity.Index, out int index) && _entities[index].Generation == entity.Generation)
        { value = _values[index]; return true; }
        value = default;
        return false;
    }
}
