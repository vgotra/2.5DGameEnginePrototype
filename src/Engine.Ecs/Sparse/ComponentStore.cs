namespace Engine.Ecs.Sparse;

public sealed class ComponentStore<T> : IComponentStore
    where T : unmanaged
{
    private const int Missing = -1;
    private T[] _components = Array.Empty<T>();
    private Entity[] _entities = Array.Empty<Entity>();
    private int[] _sparse = Array.Empty<int>();
    private int _count;

    public int Count => _count;

    internal ReadOnlySpan<Entity> DenseEntities => _entities.AsSpan(0, _count);

    internal ref T DenseComponent(int index) => ref _components[index];

    internal bool TryGetDenseIndex(Entity entity, out int denseIndex) => TryGetDenseIndexCore(entity, out denseIndex);

    public bool Has(Entity entity)
        => TryGetDenseIndex(entity, out _);

    public ref T Get(Entity entity)
    {
        if (!TryGetDenseIndexCore(entity, out int denseIndex)) throw new KeyNotFoundException("Entity does not own this component.");
        return ref _components[denseIndex];
    }

    public bool TryGet(Entity entity, out T value)
    {
        if (TryGetDenseIndexCore(entity, out int denseIndex))
        {
            value = _components[denseIndex];
            return true;
        }
        value = default;
        return false;
    }

    public void Add(Entity entity, in T component)
    {
        ValidateEntity(entity);
        EnsureSparseCapacity(entity.Id);
        int denseIndex = _sparse[entity.Id];
        if (denseIndex >= 0 && _entities[denseIndex] == entity)
        {
            _components[denseIndex] = component;
            return;
        }
        if (denseIndex >= 0) _sparse[entity.Id] = Missing;
        EnsureDenseCapacity(_count + 1);
        _entities[_count] = entity;
        _components[_count] = component;
        _sparse[entity.Id] = _count++;
    }

    public void Remove(Entity entity)
    {
        if (!TryGetDenseIndexCore(entity, out int denseIndex)) return;
        int last = --_count;
        _sparse[entity.Id] = Missing;
        if (denseIndex == last) return;
        Entity moved = _entities[last];
        _entities[denseIndex] = moved;
        _components[denseIndex] = _components[last];
        _sparse[moved.Id] = denseIndex;
    }

    private bool TryGetDenseIndexCore(Entity entity, out int denseIndex)
    {
        if (!entity.IsValid || entity.Id >= _sparse.Length)
        {
            denseIndex = Missing;
            return false;
        }
        denseIndex = _sparse[entity.Id];
        return denseIndex >= 0 && denseIndex < _count && _entities[denseIndex] == entity;
    }

    private void ValidateEntity(Entity entity)
    {
        if (!entity.IsValid) throw new ArgumentException("Entity is invalid.", nameof(entity));
    }

    private void EnsureSparseCapacity(int id)
    {
        if (id < _sparse.Length) return;
        int capacity = Math.Max(id + 1, Math.Max(4, _sparse.Length * 2));
        int oldLength = _sparse.Length;
        Array.Resize(ref _sparse, capacity);
        Array.Fill(_sparse, Missing, oldLength, capacity - oldLength);
    }

    private void EnsureDenseCapacity(int required)
    {
        if (required <= _components.Length) return;
        int capacity = Math.Max(required, Math.Max(4, _components.Length * 2));
        Array.Resize(ref _components, capacity);
        Array.Resize(ref _entities, capacity);
    }
}

internal interface IComponentStore
{
    void Remove(Entity entity);
}
