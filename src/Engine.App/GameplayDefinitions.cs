using System.Numerics;
using Engine.Ecs.Sparse;
using Engine.Rendering;

namespace Engine.App;

public enum HeroType : byte { Archer, Paladin }
public enum MonsterType : byte { Deer, Rabbit, Goblin, GoblinShaman }
public enum ItemType : byte { Unknown, HealthPotion, Gold }
public enum EffectType : byte { Impact, MuzzleFlash, SkillBurst, Pickup }

public readonly record struct HeroDefinition(
    HeroType Type,
    Vector2 Position,
    float ColliderRadius,
    TextureHandle Texture,
    Vector2 SpriteSize,
    Vector4 Color);

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
    Vector4 Color);

public readonly record struct WeaponDefinition(int Id, float Damage, float ProjectileSpeed, float ProjectileLifetime);

public readonly record struct SkillDefinition(int Id, float Cooldown, float Power);

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
