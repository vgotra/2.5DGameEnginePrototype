namespace Engine.App;

public sealed class GameplayCatalog
{
    private readonly Dictionary<HeroId, HeroDefinition> _heroes = new();
    private readonly Dictionary<EnemyId, MonsterDefinition> _enemies = new();
    private readonly Dictionary<NpcId, NpcDefinition> _npcs = new();
    private readonly Dictionary<SkillId, GameplaySkillDefinition> _skills = new();
    private readonly Dictionary<ItemId, GameplayItemDefinition> _items = new();

    public void Register(HeroId id, in HeroDefinition definition) => _heroes[id] = definition with { Id = id };

    public void RegisterDefaultHeroes()
    {
        Register(HeroIds.Rogue, HeroDefinitions.Create(HeroIds.Rogue));
        Register(HeroIds.Paladin, HeroDefinitions.Create(HeroIds.Paladin));
        Register(HeroIds.Cleric, HeroDefinitions.Create(HeroIds.Cleric));
        Register(HeroIds.Druid, HeroDefinitions.Create(HeroIds.Druid));
    }
    public void Register(EnemyId id, in MonsterDefinition definition) => _enemies[id] = definition;
    public void Register(NpcId id, in NpcDefinition definition) => _npcs[id] = definition;
    public void Register(SkillId id, in GameplaySkillDefinition definition) => _skills[id] = definition;
    public void Register(ItemId id, in GameplayItemDefinition definition) => _items[id] = definition;

    public void RegisterDefaultItems()
    {
        Register(GameContent.GoblinSlayerBow, new(GameContent.GoblinSlayerBow, EquipmentSlot.MainHand, new StatModifier { Stat = StatId.AttackPower, Amount = 7 }, default, default, new("bow-equip"), new("bow-equip")));
        Register(new ItemId("health-potion"), new(new ItemId("health-potion"), EquipmentSlot.Consumable, default, default, default, default, default));
    }

    public void RegisterDefaultSkills()
    {
        Register(SkillIds.BasicShot, new(SkillIds.BasicShot, HeroType.Archer, 1f, 0.1f, 5, default, new("basic-shot-impact"), new("basic-shot")));
        Register(SkillIds.PowerShot, new(SkillIds.PowerShot, HeroType.Archer, 2f, 0.5f, 5, default, new("power-shot-impact"), new("power-shot")));
        Register(SkillIds.ShieldStrike, new(SkillIds.ShieldStrike, HeroType.Paladin, 2f, 0.6f, 5, default, new("shield-strike-impact"), new("shield-strike")));
        Register(SkillIds.HolyStrike, new(SkillIds.HolyStrike, HeroType.Paladin, 3f, 0.9f, 5, new("holy"), new("holy-strike-impact"), new("holy-strike")));
        Register(SkillIds.Heal, new(SkillIds.Heal, HeroType.Cleric, 2f, 1.2f, 5, new("healing"), new("heal-impact"), new("heal")));
        Register(SkillIds.Smite, new(SkillIds.Smite, HeroType.Cleric, 2f, 0.8f, 5, new("smite"), new("smite-impact"), new("smite")));
        Register(SkillIds.NatureBolt, new(SkillIds.NatureBolt, HeroType.Druid, 2f, 0.5f, 5, new("nature"), new("nature-bolt-impact"), new("nature-bolt")));
        Register(SkillIds.Root, new(SkillIds.Root, HeroType.Druid, 1f, 1.1f, 5, new("root"), new("root-impact"), new("root")));
    }

    public void RegisterDefaultNpcs()
    {
        Register(GameContent.ElderMarcus, NpcDefinitions.Create(GameContent.ElderMarcus, NpcCapability.Dialogue | NpcCapability.QuestGiver, new DialogueId("elder-marcus"), quest: GameContent.GoblinProblem, brain: new BrainId("village-npc")));
        Register(GameContent.VillageBlacksmith, NpcDefinitions.Create(GameContent.VillageBlacksmith, NpcCapability.Dialogue | NpcCapability.Merchant, new DialogueId("blacksmith"), new MerchantId("blacksmith-shop"), brain: new BrainId("village-npc")));
        Register(GameContent.ClericCompanion, NpcDefinitions.Create(GameContent.ClericCompanion, NpcCapability.Companion | NpcCapability.Combatant, brain: new BrainId("cleric-companion")) with { Team = Team.Player });
    }

    public bool TryGet(HeroId id, out HeroDefinition definition) => _heroes.TryGetValue(id, out definition);
    public bool TryGet(EnemyId id, out MonsterDefinition definition) => _enemies.TryGetValue(id, out definition);
    public bool TryGet(NpcId id, out NpcDefinition definition) => _npcs.TryGetValue(id, out definition);
    public bool TryGet(SkillId id, out GameplaySkillDefinition definition) => _skills.TryGetValue(id, out definition);
    public bool TryGet(ItemId id, out GameplayItemDefinition definition) => _items.TryGetValue(id, out definition);

    public bool TryLearn(SkillId id, ref SkillKnowledge knowledge)
        => TryGet(id, out GameplaySkillDefinition definition) && knowledge.Learn(in definition);
}
