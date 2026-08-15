using Engine.App;
using Engine.Rendering;
using System.Numerics;
using Entity = Engine.Ecs.Sparse.Entity;
using SparseEntity = Engine.Ecs.Sparse.Entity;

namespace Engine.Tests;

internal static class RuntimeContractsTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Game_CreatesAndOwnsActiveWorld), Game_CreatesAndOwnsActiveWorld),
        new(nameof(World_SwitchesScenesWithoutRecreatingWorld), World_SwitchesScenesWithoutRecreatingWorld),
        new(nameof(Scene_UnloadDestroysSceneEntitiesOnly), Scene_UnloadDestroysSceneEntitiesOnly),
        new(nameof(GameplaySpawns_AreDeferredAndSceneOwned), GameplaySpawns_AreDeferredAndSceneOwned),
        new(nameof(SceneDefinitions_LoadAndEnterByTypedId), SceneDefinitions_LoadAndEnterByTypedId),
    ];

    private static void Game_CreatesAndOwnsActiveWorld()
    {
        TestGame game = new();
        World world = game.Create("Sanctuary");
        TestAssert.True(ReferenceEquals(world, game.ActiveWorld) && world.Name == "Sanctuary", "game owns the active world");
    }

    private static void World_SwitchesScenesWithoutRecreatingWorld()
    {
        TestGame game = new();
        World world = game.Create("Sanctuary");
        Scene first = world.LoadScene("Forest");
        Scene second = world.LoadScene("Cathedral");
        world.ChangeScene("Forest");
        TestAssert.True(ReferenceEquals(first, world.ActiveScene), "world switches to an already loaded scene");
        TestAssert.True(ReferenceEquals(world, game.ActiveWorld) && ReferenceEquals(second, world.LoadScene("Cathedral")), "world survives scene switching");
    }

    private static void Scene_UnloadDestroysSceneEntitiesOnly()
    {
        TestGame game = new();
        World world = game.Create("Sanctuary");
        Scene scene = world.LoadScene("Forest");
        SparseEntity sceneEntity = world.CreateEntity(EntityLifetime.Scene);
        SparseEntity transientEntity = world.CreateEntity();
        SparseEntity worldEntity = world.EcsWorld.Create();
        world.UnloadScene(scene.Name);
        TestAssert.True(!world.EcsWorld.IsAlive(sceneEntity), "scene entity is destroyed on unload");
        TestAssert.True(world.EcsWorld.IsAlive(transientEntity) && world.EcsWorld.IsAlive(worldEntity), "non-scene entities survive unload");
        TestAssert.True(!scene.IsLoaded && world.ActiveScene is null, "unloaded scene is inactive");
    }

    private static void GameplaySpawns_AreDeferredAndSceneOwned()
    {
        TestGame game = new();
        World world = game.Create("Sanctuary");
        Scene scene = world.LoadScene("Forest");
        HeroDefinition hero = new(HeroType.Archer, new Vector2(3, 4), 5, new TextureHandle(1), new Vector2(8, 9), Vector4.One);
        MonsterDefinition monster = new(MonsterType.Deer, new Vector2(7, 8), 2, 3, 4, new TextureHandle(2), new Vector2(10, 11), Vector4.One);
        Entity heroEntity = world.SpawnHero(hero);
        Entity monsterEntity = world.SpawnMonster(monster);
        Entity itemEntity = world.SpawnItem(new ItemDefinition(ItemType.Gold, new Vector2(9, 10), new TextureHandle(3), Vector2.One, Vector4.One));

        TestAssert.True(!world.EcsWorld.IsAlive(heroEntity), "gameplay spawns are deferred");
        world.ApplyCommands();
        TestAssert.True(world.EcsWorld.IsAlive(heroEntity) && world.EcsWorld.IsAlive(monsterEntity) && world.EcsWorld.IsAlive(itemEntity), "gameplay spawns activate together");
        TestAssert.True(world.EcsWorld.Get<Position>(heroEntity).Value == hero.Position, "hero definition position is preserved");
        TestAssert.True(world.EcsWorld.Get<MonsterState>(monsterEntity).Type == monster.Type, "monster definition type is preserved");
        world.UnloadScene(scene.Name);
        TestAssert.True(!world.EcsWorld.IsAlive(heroEntity) && !world.EcsWorld.IsAlive(monsterEntity) && !world.EcsWorld.IsAlive(itemEntity), "scene-owned spawns unload together");
    }

    private static void SceneDefinitions_LoadAndEnterByTypedId()
    {
        TestGame game = new();
        World world = game.Create("Sanctuary");
        SceneId sceneId = new("typed-forest");
        SceneDefinition definition = new(
            sceneId,
            new MapId("typed-forest-map"),
            new SceneMarkerDefinition("start", new Vector2(2, 3)),
            default, default, default,
            1,
            new SceneEntryPoint("entry", "start"),
            default,
            new SceneSpawnDefinition(SceneSpawnKind.None, string.Empty, default, default, default, default, default),
            default, default, default,
            1);
        Scene scene = world.LoadScene(in definition);
        TestAssert.True(scene.Map.Id == definition.Map && world.LoadScene(in definition) == scene, "typed scene loading is stable");
        MapLocation entry = world.Enter(sceneId, "entry");
        TestAssert.True(entry.Position == new Vector2(2, 3) && ReferenceEquals(world.ActiveScene, scene), "typed entry activates scene marker");
        bool rejected = false;
        try { world.Enter(sceneId, "missing"); } catch (KeyNotFoundException) { rejected = true; }
        TestAssert.True(rejected, "missing entry point is rejected");
    }

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }
}
