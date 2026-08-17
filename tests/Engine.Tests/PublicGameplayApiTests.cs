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
        new(nameof(GameAuthoringApiCreatesAndStartsScene), GameAuthoringApiCreatesAndStartsScene),
        new(nameof(PublicContentAndItemApiRemainSceneSafe), PublicContentAndItemApiRemainSceneSafe),
        new(nameof(PublicContentRegistryRejectsDuplicateIds), PublicContentRegistryRejectsDuplicateIds),
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

    private static void GameAuthoringApiCreatesAndStartsScene()
    {
        Game game = new();
        Scene scene = game.Scenes.LoadScene("Scene1");
        scene.Map.AddMarker("ForestEntrance", new Vector2(2, 3));
        scene.SetEnv(new SceneParams { Map = new MapId("Scene1"), Difficulty = 1 });
        RegisterContent(game.ActiveWorld!);

        Hero hero = scene.SpawnHero(HeroIds.Rogue, MapLocation.At("ForestEntrance"));
        Enemy enemy = scene.SpawnEnemy(TestIds.Enemy, MapLocation.At("ForestEntrance"));
        hero.Inventory.Add(TestIds.Bow);
        hero.Equipment.Equip(EquipmentSlot.MainHand, TestIds.Bow);
        hero.Skills.Learn(SkillIds.PowerShot);
        hero.Cast(SkillIds.PowerShot, enemy);

        game.StartScene("Scene1");

        TestAssert.True(game.ActiveWorld!.ActiveScene == scene, "public game API starts the requested scene");
        TestAssert.True(scene.Environment!.Difficulty == 1, "scene environment is retained");
        TestAssert.True(!hero.IsAlive, "public authoring spawns remain deferred until the fixed-step boundary");
        game.ActiveWorld!.ApplyCommands();
        TestAssert.True(hero.HasPendingCast, "public authoring commands become observable at the boundary");
    }

    private static void PublicContentAndItemApiRemainSceneSafe()
    {
        Game game = new();
        Scene scene = game.Scenes.LoadScene("items");
        scene.Map.AddMarker("loot", Vector2.One);
        game.Content.RegisterDefaultItems();

        Item item = scene.SpawnItem(GameContent.GoblinSlayerBow, MapLocation.At("loot"));

        TestAssert.True(game.Content.TryGet(GameContent.GoblinSlayerBow, out GameplayItemDefinition definition), "public content registry resolves registered items");
        TestAssert.True(definition.Id == GameContent.GoblinSlayerBow && !item.IsAlive, "item handles preserve identity and remain deferred");
        bool rejected = false;
        try
        {
            scene.SpawnItem(GameContent.GoblinSlayerBow, MapLocation.At(new MapId("other"), Vector2.Zero));
        }
        catch (ArgumentException)
        {
            rejected = true;
        }
        TestAssert.True(rejected, "wrong-map item locations are rejected");
    }

    private static void PublicContentRegistryRejectsDuplicateIds()
    {
        Game game = new();
        game.Content.RegisterHero(HeroIds.Rogue, HeroDefinitions.Create(HeroIds.Rogue));
        bool rejected = false;
        try
        {
            game.Content.RegisterHero(HeroIds.Rogue, HeroDefinitions.Create(HeroIds.Rogue));
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        TestAssert.True(rejected, "duplicate public content IDs are rejected");
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
