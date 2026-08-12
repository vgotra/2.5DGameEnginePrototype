namespace Engine.Ecs.Sparse;

public sealed class World
{
    private readonly Dictionary<Type, IComponentStore> _stores = new();

    public World()
    {
        Entities = new EntityRegistry();
    }

    public EntityRegistry Entities { get; }

    public Entity Create() => Entities.Create();

    public void AddComponent<T>(Entity entity, in T component) where T : unmanaged => Add(entity, in component);
    public void SetComponent<T>(Entity entity, in T component) where T : unmanaged => Add(entity, in component);

    public void Destroy(Entity entity)
    {
        if (!Entities.IsAlive(entity)) return;
        foreach (IComponentStore store in _stores.Values) store.Remove(entity);
        Entities.Destroy(entity);
    }

    public bool IsAlive(Entity entity) => Entities.IsAlive(entity);

    public void Add<T>(Entity entity, in T component) where T : unmanaged
    {
        RequireAlive(entity);
        GetStore<T>().Add(entity, component);
    }

    public void Remove<T>(Entity entity) where T : unmanaged
    {
        if (!Entities.IsAlive(entity)) return;
        GetStore<T>().Remove(entity);
    }

    public void RemoveComponent<T>(Entity entity) where T : unmanaged => Remove<T>(entity);

    public ref T Get<T>(Entity entity) where T : unmanaged
    {
        RequireAlive(entity);
        return ref GetStore<T>().Get(entity);
    }

    public bool TryGet<T>(Entity entity, out T value) where T : unmanaged
    {
        if (Entities.IsAlive(entity)) return GetStore<T>().TryGet(entity, out value);
        value = default;
        return false;
    }

    public bool Has<T>(Entity entity) where T : unmanaged
        => Entities.IsAlive(entity) && GetStore<T>().Has(entity);

    public Query<T> Query<T>() where T : unmanaged => new(GetStore<T>());

    public Query<T1, T2> Query<T1, T2>()
        where T1 : unmanaged
        where T2 : unmanaged
        => new(GetStore<T1>(), GetStore<T2>());

    public Query<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        => new(GetStore<T1>(), GetStore<T2>(), GetStore<T3>());

    private ComponentStore<T> GetStore<T>() where T : unmanaged
    {
        Type type = typeof(T);
        if (_stores.TryGetValue(type, out IComponentStore? existing)) return (ComponentStore<T>)existing;
        ComponentStore<T> created = new();
        _stores.Add(type, created);
        return created;
    }

    private void RequireAlive(Entity entity)
    {
        if (!Entities.IsAlive(entity)) throw new ArgumentException("Entity is not alive.", nameof(entity));
    }
}
