using Engine.Ecs.Sparse;

using System.Numerics;
namespace Engine.App;

public sealed class World
{
    private readonly Dictionary<string, Scene> _scenes = new(StringComparer.Ordinal);
    private readonly Engine.Ecs.Sparse.World _ecsWorld = new();
    private readonly EntityCommands _commands = new();
    private readonly List<GameplayCommand> _gameplayCommands = new();
    private readonly Dictionary<Entity, Inventory> _inventories = new();
    private readonly Dictionary<Entity, Equipment> _equipment = new();
    private readonly Dictionary<Entity, SkillKnowledge> _skillKnowledge = new();
    private readonly Dictionary<Entity, SkillLoadout> _skillLoadouts = new();
    private readonly Dictionary<Entity, CastRequest> _castRequests = new();
    private readonly List<Entity> _staleGameplayEntities = new();

    internal World(string name, GameplayCatalog? catalog = null)
    {
        Name = name;
        Map = new WorldMap();
        Catalog = catalog ?? new GameplayCatalog();
    }

    public string Name { get; }
    public WorldMap Map { get; }
    public GameplayCatalog Catalog { get; }
    public Scene? ActiveScene { get; private set; }
    internal Engine.Ecs.Sparse.World EcsWorld => _ecsWorld;
    internal EntityCommands Commands => _commands;

    internal Entity SpawnHero(in HeroDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new Velocity(Vector2.Zero));
        _commands.Add(entity, new Collider(definition.ColliderRadius));
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        Attributes attributes = definition.Attributes;
        CombatStats stats = definition.CombatStats.MaxHealth > 0f ? definition.CombatStats : StatSystem.Calculate(in attributes, 0, 0);
        _commands.Add(entity, attributes);
        _commands.Add(entity, stats);
        _commands.Add(entity, new HeroState { Type = definition.Type });
        InitializeGameplayState(entity);
        return RegisterSpawn(entity);
    }

    internal Entity SpawnHero(HeroId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out HeroDefinition definition)) throw new KeyNotFoundException($"Hero '{id.Value}' is not registered.");
        return SpawnHero(definition with { Position = location.Position });
    }

    internal Entity SpawnPlayer(in HeroDefinition definition) => SpawnHero(in definition);

    internal Entity SpawnMonster(in MonsterDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new MonsterState { Type = definition.Type, Speed = definition.Speed, Radius = definition.ColliderRadius });
        _commands.Add(entity, new Health(definition.Health));
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color)
        {
            BottomColor = definition.BottomColor == default ? definition.Color : definition.BottomColor
        });
        InitializeGameplayState(entity);
        return RegisterSpawn(entity);
    }

    internal Entity SpawnEnemy(EnemyId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out MonsterDefinition definition)) throw new KeyNotFoundException($"Enemy '{id.Value}' is not registered.");
        return SpawnMonster(definition with { Position = location.Position });
    }

    internal Entity SpawnNpc(in MonsterDefinition definition) => SpawnMonster(in definition);

    internal Entity SpawnNpc(in NpcDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new NpcState { Id = definition.Id, Speed = definition.Speed, Radius = definition.ColliderRadius });
        if (definition.Capabilities.Flags != 0) _commands.Add(entity, definition.Capabilities);
        if ((definition.Capabilities.Flags & NpcCapability.Companion) != 0) _commands.Add(entity, new Companion { Owner = default });
        if ((definition.Capabilities.Flags & NpcCapability.Combatant) != 0) _commands.Add(entity, new Faction { Team = definition.Team });
        _commands.Add(entity, new Health(definition.Health));
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        InitializeGameplayState(entity);
        return RegisterSpawn(entity);
    }

    internal Entity SpawnNpc(NpcId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out NpcDefinition definition)) throw new KeyNotFoundException($"NPC '{id.Value}' is not registered.");
        return SpawnNpc(definition with { Position = location.Position });
    }

    internal Entity SpawnProjectile(in ProjectileDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new ProjectileState { Direction = definition.Direction, Speed = definition.Speed, Lifetime = definition.Lifetime, Radius = definition.Radius });
        if (definition.Texture.Value != 0) _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }

    internal Entity SpawnItem(in ItemDefinition definition)
    {
        Entity entity = _commands.Create(_ecsWorld);
        _commands.Add(entity, new Position(definition.Position));
        _commands.Add(entity, new ItemState { Type = definition.Type });
        _commands.Add(entity, new Renderable(definition.Texture, definition.SpriteSize, definition.Color));
        return RegisterSpawn(entity);
    }

    internal void ApplyCommands()
    {
        _commands.Apply(_ecsWorld);
        ApplyGameplayCommands();
        PruneGameplayState();
        _commands.Clear();
        _gameplayCommands.Clear();
    }

    internal bool IsEntityAlive(Entity entity) => _ecsWorld.IsAlive(entity);

    internal Inventory GetInventory(Entity entity)
        => _inventories.TryGetValue(entity, out Inventory inventory) ? inventory : default;

    internal Equipment GetEquipment(Entity entity)
        => _equipment.TryGetValue(entity, out Equipment equipment) ? equipment : default;

    internal SkillKnowledge GetSkillKnowledge(Entity entity)
        => _skillKnowledge.TryGetValue(entity, out SkillKnowledge knowledge) ? knowledge : default;

    internal Hero CreateHeroHandle(Scene scene, HeroId id, MapLocation location)
    {
        Entity entity = SpawnHero(id, location);
        return new Hero(this, scene, entity, id);
    }

    internal Enemy CreateEnemyHandle(Scene scene, EnemyId id, MapLocation location)
    {
        Entity entity = SpawnEnemy(id, location);
        return new Enemy(this, scene, entity, id);
    }

    internal Npc CreateNpcHandle(Scene scene, NpcId id, MapLocation location)
    {
        Entity entity = SpawnNpc(id, location);
        return new Npc(this, scene, entity, id);
    }

    internal Projectile CreateProjectileHandle(in ProjectileDefinition definition)
        => new(this, SpawnProjectile(in definition));

    internal Item CreateItemHandle(ItemId id, MapLocation location)
    {
        if (!Catalog.TryGet(id, out _)) throw new KeyNotFoundException($"Item '{id.Value}' is not registered.");
        Entity entity = SpawnItem(new ItemDefinition(ItemType.Unknown, location.Position, default, Vector2.One, Vector4.One));
        return new Item(this, entity, id);
    }

    internal void QueueInventoryAdd(Entity entity, ItemId item)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.AddItem, Actor = entity, Item = item });

    internal void QueueInventoryRemove(Entity entity, ItemId item)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.RemoveItem, Actor = entity, Item = item });

    internal void QueueEquip(Entity entity, EquipmentSlot slot, ItemId item)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.Equip, Actor = entity, Slot = slot, Item = item });

    internal void QueueUnequip(Entity entity, EquipmentSlot slot)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.Unequip, Actor = entity, Slot = slot });

    internal void QueueLearnSkill(Entity entity, SkillId skill)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.LearnSkill, Actor = entity, Skill = skill });

    internal void QueueForgetSkill(Entity entity, SkillId skill)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.ForgetSkill, Actor = entity, Skill = skill });

    internal void QueueCast(Entity actor, SkillId skill, Entity target)
        => _gameplayCommands.Add(new GameplayCommand { Kind = GameplayCommandKind.Cast, Actor = actor, Target = target, Skill = skill });

    internal bool HasPendingCast(Entity actor)
        => _castRequests.TryGetValue(actor, out CastRequest request) && request.Requested;

    internal bool TryConsumeCast(Entity actor, out SkillId skill, out Entity target)
    {
        if (!_castRequests.TryGetValue(actor, out CastRequest request) || !request.Requested)
        {
            skill = default;
            target = default;
            return false;
        }

        skill = request.Skill;
        target = request.Target;
        request.Requested = false;
        _castRequests[actor] = request;
        return true;
    }

    internal void RemoveGameplayState(Entity entity)
    {
        _inventories.Remove(entity);
        _equipment.Remove(entity);
        _skillKnowledge.Remove(entity);
        _skillLoadouts.Remove(entity);
        _castRequests.Remove(entity);
    }

    private void ApplyGameplayCommands()
    {
        for (int index = 0; index < _gameplayCommands.Count; index++)
        {
            GameplayCommand command = _gameplayCommands[index];
            if (!_ecsWorld.IsAlive(command.Actor)) continue;

            switch (command.Kind)
            {
                case GameplayCommandKind.AddItem:
                    ApplyInventoryAdd(command);
                    break;
                case GameplayCommandKind.RemoveItem:
                    ApplyInventoryRemove(command);
                    break;
                case GameplayCommandKind.Equip:
                    ApplyEquip(command);
                    break;
                case GameplayCommandKind.Unequip:
                    ApplyUnequip(command);
                    break;
                case GameplayCommandKind.LearnSkill:
                    ApplyLearnSkill(command);
                    break;
                case GameplayCommandKind.ForgetSkill:
                    ApplyForgetSkill(command);
                    break;
                case GameplayCommandKind.Cast:
                    ApplyCast(command);
                    break;
            }
        }
    }

    private void ApplyInventoryAdd(in GameplayCommand command)
    {
        Inventory inventory = GetInventory(command.Actor);
        InventorySystem.TryAdd(ref inventory, command.Item, Inventory.Capacity);
        _inventories[command.Actor] = inventory;
    }

    private void ApplyInventoryRemove(in GameplayCommand command)
    {
        Inventory inventory = GetInventory(command.Actor);
        InventorySystem.TryRemove(ref inventory, command.Item);
        _inventories[command.Actor] = inventory;
    }

    private void ApplyEquip(in GameplayCommand command)
    {
        Equipment equipment = GetEquipment(command.Actor);
        Inventory inventory = GetInventory(command.Actor);
        GameplayCatalog catalog = Catalog;
        EquipmentSystem.TryEquip(ref equipment, ref inventory, command.Item, in catalog);
        _equipment[command.Actor] = equipment;
        _inventories[command.Actor] = inventory;
    }

    private void ApplyUnequip(in GameplayCommand command)
    {
        Equipment equipment = GetEquipment(command.Actor);
        Inventory inventory = GetInventory(command.Actor);
        EquipmentSystem.TryUnequip(ref equipment, ref inventory, command.Slot);
        _equipment[command.Actor] = equipment;
        _inventories[command.Actor] = inventory;
    }

    private void ApplyLearnSkill(in GameplayCommand command)
    {
        if (!Catalog.TryGet(command.Skill, out GameplaySkillDefinition definition)) return;
        SkillKnowledge knowledge = GetSkillKnowledge(command.Actor);
        knowledge.Learn(in definition);
        _skillKnowledge[command.Actor] = knowledge;
    }

    private void ApplyForgetSkill(in GameplayCommand command)
    {
        SkillKnowledge knowledge = GetSkillKnowledge(command.Actor);
        knowledge.Forget(command.Skill);
        SkillLoadout loadout = _skillLoadouts.TryGetValue(command.Actor, out SkillLoadout existingLoadout) ? existingLoadout : default;
        loadout.RemoveSkill(command.Skill);
        _skillKnowledge[command.Actor] = knowledge;
        _skillLoadouts[command.Actor] = loadout;
    }

    private void ApplyCast(in GameplayCommand command)
    {
        if (!_ecsWorld.IsAlive(command.Target) || !Catalog.TryGet(command.Skill, out _)) return;
        CastRequest request = _castRequests.TryGetValue(command.Actor, out CastRequest existingRequest) ? existingRequest : default;
        request.Skill = command.Skill;
        request.Target = command.Target;
        request.Requested = true;
        _castRequests[command.Actor] = request;
    }

    private void InitializeGameplayState(Entity entity)
    {
        _inventories[entity] = default;
        _equipment[entity] = default;
        _skillKnowledge[entity] = default;
        _skillLoadouts[entity] = default;
        _castRequests[entity] = default;
    }

    private void PruneGameplayState()
    {
        PruneState(_inventories);
        PruneState(_equipment);
        PruneState(_skillKnowledge);
        PruneState(_skillLoadouts);
        PruneState(_castRequests);
    }

    private void PruneState<T>(Dictionary<Entity, T> state)
    {
        _staleGameplayEntities.Clear();
        foreach (Entity entity in state.Keys)
            if (!_ecsWorld.IsAlive(entity)) _staleGameplayEntities.Add(entity);
        for (int index = 0; index < _staleGameplayEntities.Count; index++)
            state.Remove(_staleGameplayEntities[index]);
    }

    public Scene LoadScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_scenes.TryGetValue(name, out Scene? existing))
        {
            ActiveScene = existing;
            return existing;
        }

        Scene scene = new(name, new MapId(name), this, _ecsWorld);
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

    public Scene LoadScene(in SceneDefinition definition)
    {
        if (definition.Id.Value is null || definition.Map.Value is null) throw new ArgumentException("Scene definition requires an ID and map.");
        if (_scenes.TryGetValue(definition.Id.Value, out Scene? existing))
        {
            ActiveScene = existing;
            return existing;
        }
        Scene scene = new(definition.Id.Value, definition.Map, this, _ecsWorld);
        _scenes.Add(definition.Id.Value, scene);
        ActiveScene = scene;
        scene.Apply(in definition);
        return scene;
    }

    public void ChangeScene(SceneId id)
    {
        if (id.Value is null) throw new ArgumentException("Scene ID is required.");
        ChangeScene(id.Value);
    }

    public MapLocation Enter(SceneId id, string entryPoint)
    {
        if (id.Value is null || string.IsNullOrWhiteSpace(entryPoint)) throw new ArgumentException("Scene ID and entry point are required.");
        ChangeScene(id);
        if (ActiveScene is null || !ActiveScene.Map.TryResolve(entryPoint, out MapLocation location)) throw new KeyNotFoundException($"Scene entry point '{entryPoint}' was not found in '{id.Value}'.");
        return location;
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

    internal Entity SpawnEffect(in EffectDefinition definition)
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

    internal Effect CreateEffectHandle(in EffectDefinition definition)
        => new(this, SpawnEffect(in definition));
}
