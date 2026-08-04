using Engine.Core;
using Engine.Ecs;

namespace Engine.Tests;

internal static class EcsTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Create_EntityIsAlive), Create_EntityIsAlive),
        new(nameof(Storage_AddThenTryGet_ReturnsStoredValue), Storage_AddThenTryGet_ReturnsStoredValue),
        new(nameof(Destroy_StaleEntityIsRejected), Destroy_StaleEntityIsRejected),
        new(nameof(Destroy_PurgesComponentStorage), Destroy_PurgesComponentStorage),
        new(nameof(Create_RecyclesEntityIndex), Create_RecyclesEntityIndex),
        new(nameof(Storage_VisibleAfterIndexRecycle), Storage_VisibleAfterIndexRecycle),
        new(nameof(Storage_StaleGenerationRejectedAfterRecycle), Storage_StaleGenerationRejectedAfterRecycle),
        new(nameof(Destroy_PurgesEveryComponentStore), Destroy_PurgesEveryComponentStore),
    ];

    private static void Create_EntityIsAlive()
    {
        World world = new();
        EntityId entity = world.Create();
        TestAssert.True(world.IsAlive(entity), "entity is alive after create");
    }

    private static void Storage_AddThenTryGet_ReturnsStoredValue()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Storage<TestComponent>().Add(entity, new TestComponent(42));
        TestAssert.True(world.Storage<TestComponent>().TryGet(entity, out TestComponent value) && value.Value == 42, "component value is stored and readable");
    }

    private static void Destroy_StaleEntityIsRejected()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Destroy(entity);
        TestAssert.True(!world.IsAlive(entity), "destroyed entity is rejected as stale");
    }

    private static void Destroy_PurgesComponentStorage()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Storage<TestComponent>().Add(entity, new TestComponent(42));
        world.Destroy(entity);
        TestAssert.True(world.Storage<TestComponent>().Count == 0, "destroy purges component storage");
    }

    private static void Create_RecyclesEntityIndex()
    {
        World world = new();
        EntityId first = world.Create();
        world.Destroy(first);
        EntityId recycled = world.Create();
        TestAssert.True(recycled.Index == first.Index, "entity index is recycled after destroy");
    }

    private static void Storage_VisibleAfterIndexRecycle()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Destroy(entity);
        EntityId recycled = world.Create();
        world.Storage<TestComponent>().Add(recycled, new TestComponent(7));
        TestAssert.True(world.Storage<TestComponent>().TryGet(recycled, out TestComponent value) && value.Value == 7, "component is readable after index recycle");
    }

    private static void Storage_StaleGenerationRejectedAfterRecycle()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Destroy(entity);
        EntityId recycled = world.Create();
        world.Storage<TestComponent>().Add(recycled, new TestComponent(7));
        TestAssert.True(!world.Storage<TestComponent>().TryGet(entity, out _), "stale generation is rejected after index recycle");
    }

    private static void Destroy_PurgesEveryComponentStore()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Storage<TestComponent>().Add(entity, new TestComponent(1));
        world.Storage<TestComponent2>().Add(entity, new TestComponent2(2));
        TestAssert.True(world.Storage<TestComponent>().Count == 1 && world.Storage<TestComponent2>().Count == 1, "two component stores are populated");
        world.Destroy(entity);
        TestAssert.True(world.Storage<TestComponent>().Count == 0 && world.Storage<TestComponent2>().Count == 0, "destroy purges every component store");
    }

    internal readonly record struct TestComponent(int Value);
    internal readonly record struct TestComponent2(int Value);
}
