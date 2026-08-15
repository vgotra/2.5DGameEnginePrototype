using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public readonly record struct AbilityResult(
    bool Activated,
    ProjectileDefinition Projectile,
    EffectDefinition Effect);

public static class AbilityPipeline
{
    public static AbilityResult TryActivate(
        ref AbilityState ability,
        in SkillDefinition skill,
        in WeaponDefinition weapon,
        Vector2 origin,
        Vector2 direction,
        float effectLifetime,
        TextureHandle effectTexture,
        Vector2 effectSize)
    {
        if (ability.CooldownRemaining > 0f || direction.LengthSquared() < 0.0001f)
            return default;

        Vector2 normalized = Vector2.Normalize(direction);
        ability.CooldownRemaining = skill.Cooldown;
        return new AbilityResult(
            true,
            new ProjectileDefinition(origin, normalized, weapon.ProjectileSpeed, weapon.ProjectileLifetime, 0.2f, default, Vector2.Zero, Vector4.Zero),
            new EffectDefinition(EffectType.MuzzleFlash, origin, effectLifetime, effectTexture, effectSize, new Vector4(1f, 0.8f, 0.15f, 1f)));
    }

    public static void Tick(ref AbilityState ability, float deltaSeconds)
        => ability.CooldownRemaining = MathF.Max(0f, ability.CooldownRemaining - deltaSeconds);
}
