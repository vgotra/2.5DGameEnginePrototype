using Engine.App;
using SparseEntity = Engine.Ecs.Sparse.Entity;

namespace Engine.Tests;

internal static class RuntimeContractsTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Game_CreatesAndOwnsActiveWorld), Game_CreatesAndOwnsActiveWorld),
        new(nameof(World_SwitchesScenesWithoutRecreatingWorld), World_SwitchesScenesWithoutRecreatingWorld),
        new(nameof(Scene_UnloadDestroysSceneEntitiesOnly), Scene_UnloadDestroysSceneEntitiesOnly),
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

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }
}
