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
        Environment = new SceneParams { Map = mapId };
    }

    public string Name { get; }
    public SceneId Id => new(Name);
    public SceneMap Map { get; }
    public bool IsLoaded { get; private set; }
    public SceneParams? Environment { get; private set; }

    public void SetEnv(SceneParams parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        EnsureLoaded();
        if (parameters.Map.Value is not null && parameters.Map != Map.Id)
            throw new ArgumentException($"Scene environment map '{parameters.Map.Value}' does not match scene map '{Map.Id.Value}'.", nameof(parameters));
        Environment = parameters with { Map = Map.Id };
    }

    internal Entity SpawnMonster(in MonsterDefinition definition) => _world.SpawnMonster(in definition);

    public Hero SpawnHero(HeroId id, MapLocation location) => _world.CreateHeroHandle(this, id, ResolveLocation(location));
    public Hero SpawnHero(HeroId id, string marker) => SpawnHero(id, Map.Resolve(marker));
    public Enemy SpawnEnemy(EnemyId id, MapLocation location) => _world.CreateEnemyHandle(this, id, ResolveLocation(location));
    public Enemy SpawnEnemy(EnemyId id, string marker) => SpawnEnemy(id, Map.Resolve(marker));
    public Npc SpawnNpc(NpcId id, MapLocation location) => _world.CreateNpcHandle(this, id, ResolveLocation(location));
    public Npc SpawnNpc(NpcId id, string marker) => SpawnNpc(id, Map.Resolve(marker));
    public Item SpawnItem(ItemId id, MapLocation location) => _world.CreateItemHandle(id, ResolveLocation(location));
    public Item SpawnItem(ItemId id, string marker) => SpawnItem(id, Map.Resolve(marker));
    public Projectile SpawnProjectile(in ProjectileDefinition definition) => new Projectile(_world, _world.CreateProjectileHandle(definition).EntityHandle);
    public Effect SpawnEffect(in EffectDefinition definition) => new Effect(_world, _world.CreateEffectHandle(definition).EntityHandle);
    internal Entity SpawnItem(in ItemDefinition definition) => _world.SpawnItem(in definition);

    internal Entity SpawnEffectEntity(in EffectDefinition definition) => _world.SpawnEffect(in definition);

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
            case SceneSpawnKind.Effect: SpawnEffectEntity(spawn.Effect with { Position = Map.Resolve(spawn.Marker).Position }); break;
        }
    }

    internal void Register(Entity entity, EntityLifetime lifetime = EntityLifetime.Scene)
    {
        if (!IsLoaded) throw new InvalidOperationException("Cannot register an entity in an unloaded scene.");
        _entities[entity] = lifetime;
    }

    private MapLocation ResolveLocation(MapLocation location)
    {
        if (location.Marker is not null) return Map.Resolve(location.Marker);
        if (location.Map.Value is not null && location.Map != Map.Id)
            throw new ArgumentException($"Location map '{location.Map.Value}' does not match scene map '{Map.Id.Value}'.", nameof(location));
        return location with { Map = Map.Id };
    }

    private void EnsureLoaded()
    {
        if (!IsLoaded) throw new InvalidOperationException($"Scene '{Name}' is not loaded.");
    }

    internal void Unregister(Entity entity) => _entities.Remove(entity);

    internal void Unload()
    {
        if (!IsLoaded) return;
        foreach ((Entity entity, EntityLifetime lifetime) in _entities)
        {
            if (lifetime == EntityLifetime.Scene && _ecsWorld.IsAlive(entity)) _ecsWorld.Destroy(entity);
            _world.RemoveGameplayState(entity);
        }
        _entities.Clear();
        IsLoaded = false;
    }
}
