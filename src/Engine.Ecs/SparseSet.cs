using Engine.Core;

namespace Engine.Ecs;

/// <summary>Non-generic view of <see cref="SparseSet{T}"/> so <see cref="World"/> can purge
/// component rows from every storage on entity destruction.</summary>
public interface ISparseSet
{
    int Count { get; }
    bool Remove(EntityId entity);
}

/// <summary>
/// Dense/sparse component storage (data-oriented): a dense SoA of <see cref="EntityId"/> and value
/// rows (cache-friendly iteration, exposed as spans) indexed through a sparse array of packed dense
/// positions. Lookup is one array load, remove is swap-with-last, and growth is amortized — no
/// per-entity hash, boxing, or allocation (see Conventions/HotPath.md).
/// </summary>
public sealed class SparseSet<T> : ISparseSet where T : unmanaged
{
    private int[] _sparse = Array.Empty<int>();
    private EntityId[] _entities = Array.Empty<EntityId>();
    private T[] _values = Array.Empty<T>();
    private int _count;

    public int Count => _count;
    public ReadOnlySpan<EntityId> Entities => _entities.AsSpan(0, _count);
    public ReadOnlySpan<T> Values => _values.AsSpan(0, _count);

    private void EnsureSparse(uint index)
    {
        if (index < (uint)_sparse.Length) return;
        int newSize = _sparse.Length == 0 ? 16 : Math.Max((int)index + 1, _sparse.Length * 2);
        Array.Resize(ref _sparse, newSize);
    }

    private void EnsureDense()
    {
        if (_count < _values.Length) return;
        int newSize = _values.Length == 0 ? 16 : _values.Length * 2;
        Array.Resize(ref _entities, newSize);
        Array.Resize(ref _values, newSize);
    }

    public void Add(EntityId entity, T value)
    {
        EnsureSparse(entity.Index);
        int denseIndex = _sparse[(int)entity.Index] - 1;
        if (denseIndex >= 0) { _values[denseIndex] = value; _entities[denseIndex] = entity; return; }
        EnsureDense();
        _sparse[(int)entity.Index] = _count + 1;
        _entities[_count] = entity;
        _values[_count] = value;
        _count++;
    }

    public bool TryGet(EntityId entity, out T value)
    {
        if (entity.Index < (uint)_sparse.Length)
        {
            int denseIndex = _sparse[(int)entity.Index] - 1;
            if (denseIndex >= 0 && _entities[denseIndex].Generation == entity.Generation)
            { value = _values[denseIndex]; return true; }
        }
        value = default;
        return false;
    }

    /// <summary>Removes the entity's row if it belongs to this storage. Swap-with-last keeps the
    /// dense arrays packed (no holes) at the cost of reordering the iteration order.</summary>
    public bool Remove(EntityId entity)
    {
        if (entity.Index >= (uint)_sparse.Length) return false;
        int denseIndex = _sparse[(int)entity.Index] - 1;
        if (denseIndex < 0 || _entities[denseIndex].Generation != entity.Generation) return false;
        int last = _count - 1;
        if (denseIndex != last)
        {
            EntityId moved = _entities[last];
            _entities[denseIndex] = moved;
            _values[denseIndex] = _values[last];
            _sparse[(int)moved.Index] = denseIndex + 1;
        }
        _sparse[(int)entity.Index] = 0;
        _count--;
        return true;
    }
}
