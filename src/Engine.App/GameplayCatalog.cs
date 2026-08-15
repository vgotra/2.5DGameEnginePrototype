namespace Engine.App;

public sealed class GameplayCatalog
{
    private readonly Dictionary<HeroId, HeroDefinition> _heroes = new();
    private readonly Dictionary<EnemyId, MonsterDefinition> _enemies = new();
    private readonly Dictionary<NpcId, NpcDefinition> _npcs = new();

    public void Register(HeroId id, in HeroDefinition definition) => _heroes[id] = definition;
    public void Register(EnemyId id, in MonsterDefinition definition) => _enemies[id] = definition;
    public void Register(NpcId id, in NpcDefinition definition) => _npcs[id] = definition;

    public bool TryGet(HeroId id, out HeroDefinition definition) => _heroes.TryGetValue(id, out definition);
    public bool TryGet(EnemyId id, out MonsterDefinition definition) => _enemies.TryGetValue(id, out definition);
    public bool TryGet(NpcId id, out NpcDefinition definition) => _npcs.TryGetValue(id, out definition);
}
