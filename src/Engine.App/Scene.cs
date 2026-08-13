using Engine.Ecs.Sparse;

namespace Engine.App;

public sealed class Scene
{
    private readonly Engine.Ecs.Sparse.World _ecsWorld;
    private readonly World _world;
    private readonly Dictionary<Entity, EntityLifetime> _entities = new();

    internal Scene(string name, World world, Engine.Ecs.Sparse.World ecsWorld)
    {
        Name = name;
        _world = world;
        _ecsWorld = ecsWorld;
        IsLoaded = true;
    }

    public string Name { get; }
    public bool IsLoaded { get; private set; }

    public Entity SpawnMonster(in MonsterDefinition definition) => _world.SpawnMonster(in definition);

    public Entity SpawnEffect(in EffectDefinition definition) => _world.SpawnEffect(in definition);

    public void Register(Entity entity, EntityLifetime lifetime = EntityLifetime.Scene)
    {
        if (!IsLoaded) throw new InvalidOperationException("Cannot register an entity in an unloaded scene.");
        _entities[entity] = lifetime;
    }

    public void Unregister(Entity entity) => _entities.Remove(entity);

    internal void Unload()
    {
        if (!IsLoaded) return;
        foreach ((Entity entity, EntityLifetime lifetime) in _entities)
        {
            if (lifetime == EntityLifetime.Scene && _ecsWorld.IsAlive(entity)) _ecsWorld.Destroy(entity);
        }
        _entities.Clear();
        IsLoaded = false;
    }
}
