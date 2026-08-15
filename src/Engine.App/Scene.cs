using Engine.Ecs.Sparse;

namespace Engine.App;

public sealed class Scene
{
    private readonly Engine.Ecs.Sparse.World _ecsWorld;
    private readonly World _world;
    private readonly Dictionary<Entity, EntityLifetime> _entities = new();

    internal Scene(string name, MapId mapId, World world, Engine.Ecs.Sparse.World ecsWorld)
    {
        Name = name;
        _world = world;
        _ecsWorld = ecsWorld;
        Map = new SceneMap(mapId);
        IsLoaded = true;
    }

    public string Name { get; }
    public SceneMap Map { get; }
    public bool IsLoaded { get; private set; }

    public Entity SpawnMonster(in MonsterDefinition definition) => _world.SpawnMonster(in definition);

    public Entity SpawnHero(HeroId id, string marker) => _world.SpawnHero(id, Map.Resolve(marker));
    public Entity SpawnEnemy(EnemyId id, string marker) => _world.SpawnEnemy(id, Map.Resolve(marker));
    public Entity SpawnNpc(NpcId id, string marker) => _world.SpawnNpc(id, Map.Resolve(marker));
    public Entity SpawnItem(in ItemDefinition definition) => _world.SpawnItem(in definition);

    public Entity SpawnEffect(in EffectDefinition definition) => _world.SpawnEffect(in definition);

    internal void Apply(in SceneDefinition definition)
    {
        SceneMarkerDefinition marker1 = definition.Marker1, marker2 = definition.Marker2, marker3 = definition.Marker3, marker4 = definition.Marker4;
        SceneEntryPoint entryPoint1 = definition.EntryPoint1, entryPoint2 = definition.EntryPoint2;
        SceneSpawnDefinition spawn1 = definition.Spawn1, spawn2 = definition.Spawn2, spawn3 = definition.Spawn3, spawn4 = definition.Spawn4;
        AddMarker(in marker1, definition.MarkerCount > 0);
        AddMarker(in marker2, definition.MarkerCount > 1);
        AddMarker(in marker3, definition.MarkerCount > 2);
        AddMarker(in marker4, definition.MarkerCount > 3);
        AddEntryPoint(entryPoint1);
        AddEntryPoint(entryPoint2);
        ApplySpawn(in spawn1, definition.SpawnCount > 0);
        ApplySpawn(in spawn2, definition.SpawnCount > 1);
        ApplySpawn(in spawn3, definition.SpawnCount > 2);
        ApplySpawn(in spawn4, definition.SpawnCount > 3);
    }

    private void AddMarker(in SceneMarkerDefinition marker, bool isIncluded)
    {
        if (!isIncluded || marker.Name is null) return;
        Map.AddMarker(marker.Name, marker.Position, marker.Elevation);
    }

    private void AddEntryPoint(SceneEntryPoint entryPoint)
    {
        if (entryPoint.Name is null || entryPoint.Marker is null) return;
        if (!Map.TryResolve(entryPoint.Marker, out MapLocation location)) return;
        Map.AddMarker(entryPoint.Name, location.Position, location.Elevation);
    }

    private void ApplySpawn(in SceneSpawnDefinition spawn, bool isIncluded)
    {
        if (!isIncluded || spawn.Kind == SceneSpawnKind.None || spawn.Marker is null) return;
        switch (spawn.Kind)
        {
            case SceneSpawnKind.Hero: SpawnHero(spawn.Hero, spawn.Marker); break;
            case SceneSpawnKind.Enemy: SpawnEnemy(spawn.Enemy, spawn.Marker); break;
            case SceneSpawnKind.Npc: SpawnNpc(spawn.Npc, spawn.Marker); break;
            case SceneSpawnKind.Item: SpawnItem(spawn.Item with { Position = Map.Resolve(spawn.Marker).Position }); break;
            case SceneSpawnKind.Effect: SpawnEffect(spawn.Effect with { Position = Map.Resolve(spawn.Marker).Position }); break;
        }
    }

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
