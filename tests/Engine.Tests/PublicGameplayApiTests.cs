using System.Numerics;
using Engine.App;

namespace Engine.Tests;

internal static class PublicGameplayApiTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(SpawnedCharactersExposeTypedHandles), SpawnedCharactersExposeTypedHandles),
        new(nameof(HeroCommandsApplyInOrderAtBoundary), HeroCommandsApplyInOrderAtBoundary),
        new(nameof(CastCommandsBecomePendingGameplayIntent), CastCommandsBecomePendingGameplayIntent),
        new(nameof(SceneUnloadInvalidatesDomainHandles), SceneUnloadInvalidatesDomainHandles),
    ];

    private static void SpawnedCharactersExposeTypedHandles()
    {
        TestGame game = new();
        World world = game.Create("api");
        Scene scene = world.LoadScene("forest");
        scene.Map.AddMarker("entrance", new Vector2(2, 3));
        RegisterContent(world);

        Hero hero = scene.SpawnHero(HeroIds.Rogue, scene.Map.Resolve("entrance"));
        Enemy enemy = scene.SpawnEnemy(TestIds.Enemy, scene.Map.Resolve("entrance"));

        TestAssert.True(hero.Id == HeroIds.Rogue && enemy.Id == TestIds.Enemy, "typed handles preserve content IDs");
        TestAssert.True(!hero.IsAlive && !enemy.IsAlive, "domain spawns remain deferred");
    }

    private static void HeroCommandsApplyInOrderAtBoundary()
    {
        TestGame game = new();
        World world = game.Create("commands");
        Scene scene = world.LoadScene("forest");
        scene.Map.AddMarker("entrance", new Vector2(2, 3));
        RegisterContent(world);

        Hero hero = scene.SpawnHero(HeroIds.Rogue, scene.Map.Resolve("entrance"));
        hero.Inventory.Add(TestIds.Bow);
        hero.Equipment.Equip(EquipmentSlot.MainHand, TestIds.Bow);
        hero.Skills.Learn(SkillIds.PowerShot);

        TestAssert.True(hero.Inventory.Count == 0, "commands do not mutate before the fixed-step boundary");
        world.ApplyCommands();

        TestAssert.True(hero.IsAlive, "spawned hero activates at the boundary");
        TestAssert.True(hero.Equipment.Get(EquipmentSlot.MainHand) == TestIds.Bow, "equipment command applies");
        TestAssert.True(!hero.Inventory.Contains(TestIds.Bow), "equipped item leaves inventory");
        TestAssert.True(hero.Skills.IsKnown(SkillIds.PowerShot), "skill command applies");
    }

    private static void CastCommandsBecomePendingGameplayIntent()
    {
        TestGame game = new();
        World world = game.Create("combat");
        Scene scene = world.LoadScene("forest");
        scene.Map.AddMarker("entrance", new Vector2(2, 3));
        RegisterContent(world);

        Hero hero = scene.SpawnHero(HeroIds.Rogue, scene.Map.Resolve("entrance"));
        Enemy enemy = scene.SpawnEnemy(TestIds.Enemy, scene.Map.Resolve("entrance"));
        hero.Cast(SkillIds.PowerShot, enemy);
        world.ApplyCommands();

        TestAssert.True(hero.HasPendingCast, "cast command becomes a pending gameplay intent");
    }

    private static void SceneUnloadInvalidatesDomainHandles()
    {
        TestGame game = new();
        World world = game.Create("unload");
        Scene scene = world.LoadScene("forest");
        scene.Map.AddMarker("entrance", new Vector2(2, 3));
        RegisterContent(world);

        Hero hero = scene.SpawnHero(HeroIds.Rogue, scene.Map.Resolve("entrance"));
        world.ApplyCommands();
        world.UnloadScene(scene.Name);

        TestAssert.True(!hero.IsAlive && hero.Inventory.Count == 0, "scene unload invalidates and clears domain state");
    }

    private static void RegisterContent(World world)
    {
        world.Catalog.RegisterDefaultHeroes();
        world.Catalog.RegisterDefaultItems();
        world.Catalog.RegisterDefaultSkills();
        world.Catalog.Register(TestIds.Enemy, new MonsterDefinition(
            MonsterType.Goblin,
            default,
            1f,
            0.3f,
            10,
            default,
            new Vector2(16, 20),
            Vector4.One));
    }

    private static class TestIds
    {
        internal static readonly EnemyId Enemy = new("test-enemy");
        internal static readonly ItemId Bow = GameContent.GoblinSlayerBow;
    }

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }
}
