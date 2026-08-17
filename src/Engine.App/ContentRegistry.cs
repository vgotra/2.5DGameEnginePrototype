namespace Engine.App;

public sealed class ContentRegistry
{
    private readonly GameplayCatalog _catalog;
    private readonly HashSet<HeroId> _heroes = new();
    private readonly HashSet<EnemyId> _enemies = new();
    private readonly HashSet<NpcId> _npcs = new();
    private readonly HashSet<ItemId> _items = new();
    private readonly HashSet<SkillId> _skills = new();
    private readonly HashSet<EffectId> _effects = new();
    private readonly HashSet<QuestId> _quests = new();
    private readonly Dictionary<SceneId, SceneDefinition> _scenes = new();
    private readonly Dictionary<WorldMapLocationId, WorldLocation> _worldLocations = new();

    internal ContentRegistry(GameplayCatalog catalog) => _catalog = catalog;

    public void RegisterHero(HeroId id, in HeroDefinition definition)
    {
        EnsureNew(_heroes, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterEnemy(EnemyId id, in MonsterDefinition definition)
    {
        EnsureNew(_enemies, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterNpc(NpcId id, in NpcDefinition definition)
    {
        EnsureNew(_npcs, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterItem(ItemId id, in GameplayItemDefinition definition)
    {
        EnsureNew(_items, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterSkill(SkillId id, in GameplaySkillDefinition definition)
    {
        EnsureNew(_skills, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterEffect(EffectId id, in GameplayEffectDefinition definition)
    {
        EnsureNew(_effects, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterQuest(QuestId id, in QuestDefinition definition)
    {
        EnsureNew(_quests, id);
        _catalog.Register(id, in definition);
    }

    public void RegisterScene(SceneId id, in SceneDefinition definition)
    {
        if (!_scenes.TryAdd(id, definition)) throw new InvalidOperationException($"Content ID '{id}' is already registered.");
    }

    public void RegisterWorldLocation(WorldMapLocationId id, in WorldLocation location)
    {
        if (!_worldLocations.TryAdd(id, location)) throw new InvalidOperationException($"World location '{id}' is already registered.");
    }

    public bool TryGet(SceneId id, out SceneDefinition definition) => _scenes.TryGetValue(id, out definition);
    public bool TryGet(WorldMapLocationId id, out WorldLocation location) => _worldLocations.TryGetValue(id, out location);
    public void RegisterDefaultHeroes()
    {
        RegisterHero(HeroIds.Rogue, HeroDefinitions.Create(HeroIds.Rogue));
        RegisterHero(HeroIds.Paladin, HeroDefinitions.Create(HeroIds.Paladin));
        RegisterHero(HeroIds.Cleric, HeroDefinitions.Create(HeroIds.Cleric));
        RegisterHero(HeroIds.Druid, HeroDefinitions.Create(HeroIds.Druid));
    }

    public void RegisterDefaultItems()
    {
        RegisterItem(GameContent.GoblinSlayerBow, new(GameContent.GoblinSlayerBow, EquipmentSlot.MainHand, new StatModifier { Stat = StatId.AttackPower, Amount = 7 }, default, default, new("bow-equip"), new("bow-equip")));
        RegisterItem(new ItemId("health-potion"), new(new ItemId("health-potion"), EquipmentSlot.Consumable, default, default, default, default, default));
    }

    public void RegisterDefaultSkills()
    {
        if (_skills.Count != 0) throw new InvalidOperationException("Default skills are already registered.");
        GameplayCatalog catalog = _catalog;
        catalog.RegisterDefaultSkills();
        _skills.UnionWith([SkillIds.BasicShot, SkillIds.PowerShot, SkillIds.ShieldStrike, SkillIds.HolyStrike, SkillIds.Heal, SkillIds.Smite, SkillIds.NatureBolt, SkillIds.Root, SkillIds.PoisonArrow]);
    }

    public void RegisterDefaultNpcs()
    {
        if (_npcs.Count != 0) throw new InvalidOperationException("Default NPCs are already registered.");
        catalogRegisterDefaultNpcs();
    }

    internal void ApplyWorldMap(WorldMap worldMap)
    {
        foreach (WorldLocation location in _worldLocations.Values) worldMap.Register(location);
    }
    public bool TryGet(HeroId id, out HeroDefinition definition) => _catalog.TryGet(id, out definition);
    public bool TryGet(EnemyId id, out MonsterDefinition definition) => _catalog.TryGet(id, out definition);
    public bool TryGet(SkillId id, out GameplaySkillDefinition definition) => _catalog.TryGet(id, out definition);
    public bool TryGet(ItemId id, out GameplayItemDefinition definition) => _catalog.TryGet(id, out definition);
    public bool TryGet(EffectId id, out GameplayEffectDefinition definition) => _catalog.TryGet(id, out definition);
    public bool TryGet(QuestId id, out QuestDefinition definition) => _catalog.TryGet(id, out definition);

    private static void EnsureNew<T>(HashSet<T> registered, T id)
    {
        if (!registered.Add(id)) throw new InvalidOperationException($"Content ID '{id}' is already registered.");
    }

    private void catalogRegisterDefaultNpcs()
    {
        _catalog.RegisterDefaultNpcs();
        _npcs.UnionWith([GameContent.ElderMarcus, GameContent.VillageBlacksmith, GameContent.ClericCompanion]);
    }

}
