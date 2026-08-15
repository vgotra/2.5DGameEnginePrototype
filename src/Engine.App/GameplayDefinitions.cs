using System.Numerics;
using Engine.Ecs.Sparse;
using Engine.Rendering;

namespace Engine.App;

public enum HeroType : byte { Archer, Paladin, Cleric, Druid }
public enum MonsterType : byte { Deer, Rabbit, Goblin, GoblinShaman }
public enum ItemType : byte { Unknown, HealthPotion, Gold }
public enum EffectType : byte { Impact, MuzzleFlash, SkillBurst, Pickup }

public readonly record struct HeroDefinition(
    HeroType Type,
    Vector2 Position,
    float ColliderRadius,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color)
{
    public HeroId Id { get; init; }
    public Attributes Attributes { get; init; }
    public CombatStats CombatStats { get; init; }
}

public static class HeroDefinitions
{
    public static HeroDefinition Create(HeroId id)
    {
        HeroDefinition definition = id == HeroIds.Paladin
            ? new(HeroType.Paladin, default, 0.4f, default, new(40, 48), new(0.8f, 0.8f, 1f, 1f)) { Attributes = new Attributes { Strength = 6, Dexterity = 2, Intelligence = 1, Vitality = 7, Spirit = 2 } }
            : id == HeroIds.Cleric
                ? new(HeroType.Cleric, default, 0.4f, default, new(40, 48), new(0.8f, 1f, 1f, 1f)) { Attributes = new Attributes { Strength = 1, Dexterity = 2, Intelligence = 6, Vitality = 3, Spirit = 7 } }
                : id == HeroIds.Druid
                    ? new(HeroType.Druid, default, 0.4f, default, new(40, 48), new(0.5f, 0.9f, 0.5f, 1f)) { Attributes = new Attributes { Strength = 2, Dexterity = 3, Intelligence = 5, Vitality = 4, Spirit = 6 } }
                    : new(HeroType.Archer, default, 0.4f, default, new(40, 48), Vector4.One) { Attributes = new Attributes { Strength = 2, Dexterity = 7, Intelligence = 2, Vitality = 3, Spirit = 3 } };
        Attributes attributes = definition.Attributes;
        return definition with { Id = id, CombatStats = StatSystem.Calculate(in attributes, 0, 0) };
    }
}

public readonly record struct MonsterDefinition(
    MonsterType Type,
    Vector2 Position,
    float Speed,
    float ColliderRadius,
    int Health,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color);

public readonly record struct NpcDefinition(
    int Id,
    Vector2 Position,
    float Speed,
    float ColliderRadius,
    int Health,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color)
{
    public NpcId GameplayId { get; init; }
    public NpcCapabilities Capabilities { get; init; }
    public DialogueId Dialogue { get; init; }
    public MerchantId Merchant { get; init; }
    public QuestId Quest { get; init; }
    public BrainId Brain { get; init; }
    public Team Team { get; init; }
}

public static class NpcDefinitions
{
    public static NpcDefinition Create(NpcId id, NpcCapability capabilities, DialogueId dialogue = default, MerchantId merchant = default, QuestId quest = default, BrainId brain = default)
        => new(0, default, 1f, 0.35f, 20, default, new Vector2(34, 44), Vector4.One)
        {
            GameplayId = id,
            Capabilities = new NpcCapabilities(capabilities),
            Dialogue = dialogue,
            Merchant = merchant,
            Quest = quest,
            Brain = brain
        };
}

public readonly record struct WeaponDefinition(int Id, float Damage, float ProjectileSpeed, float ProjectileLifetime);

public readonly record struct SkillDefinition(int Id, float Cooldown, float Power);

public readonly record struct GameplaySkillDefinition(
    SkillId Id,
    HeroType Hero,
    float Power,
    float Cooldown,
    int MaximumLevel,
    EffectId Effect,
    VfxId ImpactVfx,
    SoundId CastSound);

public readonly record struct GameplayItemDefinition(
    ItemId Id,
    EquipmentSlot Slot,
    StatModifier FirstModifier,
    StatModifier SecondModifier,
    EffectId Effect,
    VfxId EquipVfx,
    SoundId EquipSound);

public readonly record struct EffectDefinition(
    EffectType Type,
    Vector2 Position,
    float Lifetime,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color);

public readonly record struct ProjectileDefinition(
    Vector2 Position,
    Vector2 Direction,
    float Speed,
    float Lifetime,
    float Radius,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color);

public readonly record struct ItemDefinition(
    ItemType Type,
    Vector2 Position,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color);

public struct HeroState
{
    public HeroType Type;
}

public struct MonsterState
{
    public MonsterType Type;
    public float Speed;
    public float Radius;
    public Vector2 WanderTarget;
}

public struct NpcState
{
    public int Id;
    public float Speed;
    public float Radius;
    public byte Behavior;
    public Vector2 WanderTarget;
}

public struct ProjectileState
{
    public Vector2 Direction;
    public float Speed;
    public float Lifetime;
    public float Radius;
}

public struct ItemState
{
    public ItemType Type;
}

public struct AbilityState
{
    public int Id;
    public float CooldownRemaining;
}

public struct AbilityInput
{
    public int AbilityId;
    public bool Requested;
}

public struct Damage
{
    public int Amount;
    public Entity Source;
}

public struct WeaponState
{
    public int Id;
    public float Damage;
    public float ProjectileSpeed;
    public float ProjectileLifetime;
}

public struct EffectState
{
    public EffectType Type;
    public float LifetimeRemaining;
}

public struct Lifetime
{
    public float Remaining;
}

public struct VfxState
{
    public float Time;
    public float Duration;
    public float Scale;
    public float Opacity;
}
