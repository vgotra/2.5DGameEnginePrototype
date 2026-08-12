using Engine.Core;

namespace Engine.App;

public sealed class Scene
{
    private readonly Engine.Ecs.World _ecsWorld;
    private readonly Dictionary<EntityId, EntityLifetime> _entities = new();

    internal Scene(string name, Engine.Ecs.World ecsWorld)
    {
        Name = name;
        _ecsWorld = ecsWorld;
        IsLoaded = true;
    }

    public string Name { get; }
    public bool IsLoaded { get; private set; }

    public void Register(EntityId entity, EntityLifetime lifetime = EntityLifetime.Scene)
    {
        if (!IsLoaded) throw new InvalidOperationException("Cannot register an entity in an unloaded scene.");
        _entities[entity] = lifetime;
    }

    public void Unregister(EntityId entity) => _entities.Remove(entity);

    internal void Unload()
    {
        if (!IsLoaded) return;
        foreach ((EntityId entity, EntityLifetime lifetime) in _entities)
        {
            if (lifetime == EntityLifetime.Scene && _ecsWorld.IsAlive(entity)) _ecsWorld.Destroy(entity);
        }
        _entities.Clear();
        IsLoaded = false;
    }
}
