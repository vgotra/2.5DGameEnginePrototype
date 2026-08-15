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
public readonly record struct DialogueId(string Value);
public readonly record struct MerchantId(string Value);
public readonly record struct BrainId(string Value);

public static class HeroIds
{
    public static readonly HeroId Rogue = new("rogue");
    public static readonly HeroId Paladin = new("paladin");
    public static readonly HeroId Cleric = new("cleric");
    public static readonly HeroId Druid = new("druid");
}

public static class SkillIds
{
    public static readonly SkillId BasicShot = new("basic-shot");
    public static readonly SkillId PowerShot = new("power-shot");
    public static readonly SkillId ShieldStrike = new("shield-strike");
    public static readonly SkillId HolyStrike = new("holy-strike");
    public static readonly SkillId Heal = new("heal");
    public static readonly SkillId Smite = new("smite");
    public static readonly SkillId NatureBolt = new("nature-bolt");
    public static readonly SkillId Root = new("root");
    public static readonly SkillId PoisonArrow = new("poison-arrow");
}
