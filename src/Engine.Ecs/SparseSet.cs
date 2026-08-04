using Engine.Core;

namespace Engine.Ecs;

/// <summary>Non-generic view of <see cref="SparseSet{T}"/> so <see cref="World"/> can purge
/// component rows from every storage on entity destruction.</summary>
public interface ISparseSet
{
    int Count { get; }
    bool Remove(EntityId entity);
}

public sealed class SparseSet<T> : ISparseSet where T : unmanaged
{
    private readonly Dictionary<uint, int> _indices = new();
    private readonly List<EntityId> _entities = new();
    private readonly List<T> _values = new();

    public int Count => _values.Count;
    public ReadOnlySpan<EntityId> Entities => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_entities);
    public ReadOnlySpan<T> Values => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_values);

    public void Add(EntityId entity, T value)
    {
        if (_indices.TryGetValue(entity.Index, out int index)) { _values[index] = value; _entities[index] = entity; return; }
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

    /// <summary>Removes the entity's row if it belongs to this storage. Swap-with-last keeps the
    /// backing lists dense (no holes) at the cost of reordering the iteration order.</summary>
    public bool Remove(EntityId entity)
    {
        if (!_indices.TryGetValue(entity.Index, out int index) || _entities[index].Generation != entity.Generation) return false;
        int last = _values.Count - 1;
        if (index != last)
        {
            EntityId moved = _entities[last];
            _values[index] = _values[last];
            _entities[index] = moved;
            _indices[moved.Index] = index;
        }
        _values.RemoveAt(last);
        _entities.RemoveAt(last);
        _indices.Remove(entity.Index);
        return true;
    }
}
