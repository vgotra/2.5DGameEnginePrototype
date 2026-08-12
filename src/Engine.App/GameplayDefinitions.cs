using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public enum HeroType : byte { Archer, Paladin }
public enum MonsterType : byte { Deer, Rabbit, Goblin, GoblinShaman }
public enum ItemType : byte { Unknown, HealthPotion, Gold }

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

public readonly record struct WeaponDefinition(int Id, float Damage, float ProjectileSpeed, float ProjectileLifetime);

public readonly record struct SkillDefinition(int Id, float Cooldown, float Power);

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
