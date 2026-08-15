namespace Engine.App;

public readonly record struct HeroId(string Value);
public readonly record struct EnemyId(string Value);
public readonly record struct NpcId(string Value);
public readonly record struct SkillId(string Value);
public readonly record struct ItemId(string Value);
public readonly record struct EffectId(string Value);
public readonly record struct QuestId(string Value);
public readonly record struct SceneId(string Value);
public readonly record struct MapId(string Value);
public readonly record struct VfxId(string Value);
public readonly record struct SoundId(string Value);

public static class HeroIds
{
    public static readonly HeroId Rogue = new("rogue");
    public static readonly HeroId Paladin = new("paladin");
    public static readonly HeroId Cleric = new("cleric");
    public static readonly HeroId Druid = new("druid");
}
