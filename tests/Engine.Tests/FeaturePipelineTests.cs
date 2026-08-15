using System.Numerics;
using Engine.App;
using Engine.Rendering;
using Entity = Engine.Ecs.Sparse.Entity;

namespace Engine.Tests;

internal static class FeaturePipelineTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(RenderItem_PreservesEffectMetadata), RenderItem_PreservesEffectMetadata),
        new(nameof(VfxPool_ReusesExpiredSlots), VfxPool_ReusesExpiredSlots),
        new(nameof(VfxPool_MuzzleFlashUsesPresentationOffset), VfxPool_MuzzleFlashUsesPresentationOffset),
        new(nameof(AbilityPipeline_EnforcesCooldown), AbilityPipeline_EnforcesCooldown),
        new(nameof(EffectLifetime_DestroysAfterDuration), EffectLifetime_DestroysAfterDuration),
    ];

    private static void RenderItem_PreservesEffectMetadata()
    {
        RenderItem item = new(Vector2.One, new(2, 3), new(4), Vector4.One)
        {
            Scale = 1.5f,
            AnimationFrame = 3,
            Blend = BlendMode.Additive,
            Material = new MaterialHandle(7),
        };
        TestAssert.True(item.Scale == 1.5f && item.AnimationFrame == 3 && item.Blend == BlendMode.Additive && item.Material.Value == 7, "render metadata is preserved");
    }

    private static void VfxPool_ReusesExpiredSlots()
    {
        VfxPool pool = new(1);
        EffectDefinition definition = new(EffectType.Impact, Vector2.Zero, 0.1f, default, Vector2.One, Vector4.One);
        TestAssert.True(pool.TryAcquire(in definition, out int first), "first VFX slot acquired");
        TestAssert.True(!pool.TryAcquire(in definition, out _), "pool capacity is enforced");
        pool.Update(0.1f);
        TestAssert.True(pool.TryAcquire(in definition, out int second) && first == second, "expired slot is reused");
    }

    private static void VfxPool_MuzzleFlashUsesPresentationOffset()
    {
        VfxPool pool = new(2);
        EffectDefinition definition = new(EffectType.MuzzleFlash, new Vector2(3, 4), 1f, default, new Vector2(8, 8), Vector4.One);
        TestAssert.True(pool.TryAcquire(in definition, out _), "muzzle flash acquired");
        RenderItem[] items = new RenderItem[2];
        int count = pool.Extract(items, new Vector2(0, -28));
        TestAssert.True(count == 1 && items[0].ScreenOffset == new Vector2(0, -28), "muzzle flash uses render-only center offset");
    }

    private static void AbilityPipeline_EnforcesCooldown()
    {
        AbilityState state = default;
        SkillDefinition skill = new(SkillIds.PowerShot, 0.5f, 2f);
        WeaponDefinition weapon = new(GameContent.GoblinSlayerBow, 5f, 10f, 1f);
        AbilityResult result = AbilityPipeline.TryActivate(ref state, in skill, in weapon, Vector2.Zero, Vector2.UnitX, 0.1f, default, Vector2.One);
        TestAssert.True(result.Activated && state.CooldownRemaining == 0.5f, "ability activates and starts cooldown");
        TestAssert.True(!AbilityPipeline.TryActivate(ref state, in skill, in weapon, Vector2.Zero, Vector2.UnitX, 0.1f, default, Vector2.One).Activated, "cooldown blocks repeat activation");
        AbilityPipeline.Tick(ref state, 0.5f);
        TestAssert.True(AbilityPipeline.TryActivate(ref state, in skill, in weapon, Vector2.Zero, Vector2.UnitX, 0.1f, default, Vector2.One).Activated, "cooldown expires");
    }

    private static void EffectLifetime_DestroysAfterDuration()
    {
        TestGame game = new();
        World world = game.Create("Test");
        world.LoadScene("Scene");
        Entity effect = world.SpawnEffect(new EffectDefinition(EffectType.Impact, Vector2.Zero, 0.1f, default, Vector2.One, Vector4.One));
        world.ApplyCommands();
        LifetimeSystem system = new() { Buffer = world.Commands };
        system.Update(world.EcsWorld, 0.1f);
        world.ApplyCommands();
        TestAssert.True(!world.EcsWorld.IsAlive(effect), "expired effect is destroyed through deferred commands");
    }

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }
}
