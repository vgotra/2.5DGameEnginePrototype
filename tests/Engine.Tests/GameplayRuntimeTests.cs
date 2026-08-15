using Engine.App;

namespace Engine.Tests;

internal static class GameplayRuntimeTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Stats_IncludeAttributesAndEquipment), Stats_IncludeAttributesAndEquipment),
        new(nameof(Inventory_RespectsCapacity), Inventory_RespectsCapacity),
        new(nameof(Combat_AppliesDamageAndExpiresEffects), Combat_AppliesDamageAndExpiresEffects),
        new(nameof(Scene_TypedMarkerSpawnsRemainDeferred), Scene_TypedMarkerSpawnsRemainDeferred)
    ];

    private static void Stats_IncludeAttributesAndEquipment()
    {
        Attributes attributes = new() { Strength = 3, Dexterity = 4, Vitality = 5, Spirit = 2 };
        CombatStats stats = StatSystem.Calculate(in attributes, 7, 2);
        TestAssert.True(stats.MaxHealth == 150f && stats.AttackPower == 17f && stats.Armor == 7f, "derived stats include base attributes and modifiers");
    }

    private static void Inventory_RespectsCapacity()
    {
        Inventory inventory = default;
        ItemId item = new("bow");
        TestAssert.True(InventorySystem.TryAdd(ref inventory, item, 1), "first item fits");
        TestAssert.True(!InventorySystem.TryAdd(ref inventory, item, 1), "capacity blocks additional item");
        TestAssert.True(InventorySystem.TryRemove(ref inventory, item) && inventory.Count == 0, "item can be removed");
    }

    private static void Combat_AppliesDamageAndExpiresEffects()
    {
        Health health = new(10);
        GameplayEffect effect = new() { Id = new EffectId("poison"), RemainingTicks = 1 };
        TestAssert.True(CombatSystem.TryApply(ref health, ref effect, 4) && health.Value == 6, "combat applies damage");
        CombatSystem.TickEffect(ref effect);
        TestAssert.True(effect.Stacks == 0, "effect expires deterministically");
    }

    private static void Scene_TypedMarkerSpawnsRemainDeferred()
    {
        TestGame game = new();
        World world = game.Create("world");
        Scene scene = world.LoadScene("village");
        scene.Map.AddMarker("start", new System.Numerics.Vector2(2, 3));
        world.Catalog.Register(HeroIds.Rogue, new HeroDefinition(HeroType.Archer, default, 1, default, System.Numerics.Vector2.One, System.Numerics.Vector4.One));
        var entity = scene.SpawnHero(HeroIds.Rogue, "start");
        TestAssert.True(!world.EcsWorld.IsAlive(entity), "typed scene spawn is deferred");
    }

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }
}
