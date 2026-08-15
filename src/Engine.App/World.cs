using Engine.Ecs.Sparse;

using System.Numerics;
namespace Engine.App;

public sealed class World
{
    private readonly Dictionary<string, Scene> _scenes = new(StringComparer.Ordinal);
    private readonly Engine.Ecs.Sparse.World _ecsWorld = new();
    private readonly EntityCommands _commands = new();

    internal World(string name)
    {
        Name = name;
        Map = new WorldMap();
    }

    public string Name { get; }
    public WorldMap Map { get; }
    public GameplayCatalog Catalog { get; } = new();
    public Scene? ActiveScene { get; private set; }
    public Engine.Ecs.Sparse.World EcsWorld => _ecsWorld;
    public EntityCommands Commands => _commands;

    public Entity SpawnHero(in HeroDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new Velocity(Vector2.Zero));
        _commands.Add(entity, new Collider(definition.ColliderRadius));
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        _commands.Add(entity, new HeroState { Type = definition.Type });
        return RegisterSpawn(entity);
    }

    public Entity SpawnHero(HeroId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out HeroDefinition definition)) throw new KeyNotFoundException($"Hero '{id.Value}' is not registered.");
        return SpawnHero(definition with { Position = location.Position });
    }

    public Entity SpawnPlayer(in HeroDefinition definition) => SpawnHero(in definition);

    public Entity SpawnMonster(in MonsterDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new MonsterState { Type = definition.Type, Speed = definition.Speed, Radius = definition.ColliderRadius });
        _commands.Add(entity, new Health(definition.Health));
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }

    public Entity SpawnEnemy(EnemyId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out MonsterDefinition definition)) throw new KeyNotFoundException($"Enemy '{id.Value}' is not registered.");
        return SpawnMonster(definition with { Position = location.Position });
    }

    public Entity SpawnNpc(in MonsterDefinition definition) => SpawnMonster(in definition);

    public Entity SpawnNpc(in NpcDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new NpcState { Id = definition.Id, Speed = definition.Speed, Radius = definition.ColliderRadius });
        _commands.Add(entity, new Health(definition.Health));
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }

    public Entity SpawnNpc(NpcId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out NpcDefinition definition)) throw new KeyNotFoundException($"NPC '{id.Value}' is not registered.");
        return SpawnNpc(definition with { Position = location.Position });
    }

    public Entity SpawnProjectile(in ProjectileDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new ProjectileState { Direction = definition.Direction, Speed = definition.Speed, Lifetime = definition.Lifetime, Radius = definition.Radius });
        if (definition.Texture.Value != 0) _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }

    public Entity SpawnItem(in ItemDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new ItemState { Type = definition.Type });
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }

    public void ApplyCommands()
    {
        _commands.Apply(_ecsWorld);
        _commands.Clear();
    }

    public Scene LoadScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_scenes.TryGetValue(name, out Scene? existing))
        {
            ActiveScene = existing;
            return existing;
        }

        Scene scene = new(name, this, _ecsWorld);
        _scenes.Add(name, scene);
        ActiveScene = scene;
        return scene;
    }

    public void ChangeScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_scenes.TryGetValue(name, out Scene? scene)) throw new KeyNotFoundException($"Scene '{name}' is not loaded.");
        ActiveScene = scene;
    }

    public void UnloadScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_scenes.Remove(name, out Scene? scene)) return;
        scene.Unload();
        if (ReferenceEquals(ActiveScene, scene)) ActiveScene = null;
    }

    public Entity CreateEntity(EntityLifetime lifetime = EntityLifetime.Transient)
    {
        Entity entity = _ecsWorld.Create();
        if (lifetime == EntityLifetime.Scene)
        {
            if (ActiveScene is null) throw new InvalidOperationException("A scene is required for scene-owned entities.");
            ActiveScene.Register(entity, lifetime);
        }
        return entity;
    }

    private Entity RegisterSpawn(Entity entity)
    {
        if (ActiveScene is not null) ActiveScene.Register(entity, EntityLifetime.Scene);
        return entity;
    }

    public Entity SpawnEffect(in EffectDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new EffectState { Type = definition.Type, LifetimeRemaining = definition.Lifetime });
        _commands.Add(entity, new Lifetime { Remaining = definition.Lifetime });
        _commands.Add(entity, new VfxState { Duration = definition.Lifetime, Scale = 1f, Opacity = 1f });
        if (definition.Texture.Value != 0)
            _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }
}
