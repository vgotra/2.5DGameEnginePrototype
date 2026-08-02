using Engine.Core;

namespace Engine.Ecs;

public sealed class World
{
    private readonly List<ushort> _generations = new();
    private readonly Stack<uint> _free = new();
    private readonly Dictionary<ComponentTypeId, object> _stores = new();

    public EntityId Create()
    {
        if (_free.Count > 0) { uint index = _free.Pop(); return new(index, _generations[(int)index]); }
        uint next = (uint)_generations.Count;
        _generations.Add(1);
        return new(next, 1);
    }

    public void Destroy(EntityId entity)
    {
        if (!IsAlive(entity)) return;
        _generations[(int)entity.Index]++;
        _free.Push(entity.Index);
        foreach (object store in _stores.Values) ((ISparseSet)store).Remove(entity);
    }

    public bool IsAlive(EntityId entity)
        => entity.IsValid && entity.Index < _generations.Count && _generations[(int)entity.Index] == entity.Generation;

    public SparseSet<T> Storage<T>() where T : unmanaged
    {
        ComponentTypeId id = ComponentTypeId.Of<T>();
        if (_stores.TryGetValue(id, out object? existing)) return (SparseSet<T>)existing;
        SparseSet<T> created = new();
        _stores.Add(id, created);
        return created;
    }

    public bool TryGetStorage<T>(out SparseSet<T>? storage) where T : unmanaged
    {
        if (_stores.TryGetValue(ComponentTypeId.Of<T>(), out object? existing)) { storage = (SparseSet<T>)existing; return true; }
        storage = null;
        return false;
    }
}
