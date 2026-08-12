using Engine.Ecs.Sparse;

namespace Engine.Tests;

internal static class SparseEcsTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(CreateDestroyRecycle_IsGenerationSafe), CreateDestroyRecycle_IsGenerationSafe),
        new(nameof(ComponentOperations_ReadWriteAndRemove), ComponentOperations_ReadWriteAndRemove),
        new(nameof(AddExisting_UpdatesWithoutDuplicate), AddExisting_UpdatesWithoutDuplicate),
        new(nameof(Remove_SwapKeepsMovedComponent), Remove_SwapKeepsMovedComponent),
        new(nameof(Destroy_RemovesEveryComponent), Destroy_RemovesEveryComponent),
        new(nameof(MultipleTypes_UseIndependentStores), MultipleTypes_UseIndependentStores),
        new(nameof(CapacityGrowth_PreservesSparseLookups), CapacityGrowth_PreservesSparseLookups),
        new(nameof(DefaultEntity_IsRejected), DefaultEntity_IsRejected),
        new(nameof(SteadyStateLookup_IsAllocationFree), SteadyStateLookup_IsAllocationFree),
        new(nameof(Queries_CountAndIterateIntersections), Queries_CountAndIterateIntersections),
        new(nameof(QueryRefMutation_UpdatesComponents), QueryRefMutation_UpdatesComponents),
        new(nameof(Query_ReflectsStructuralChanges), Query_ReflectsStructuralChanges),
        new(nameof(QueryIteration_IsAllocationFree), QueryIteration_IsAllocationFree),
    ];

    private static void CreateDestroyRecycle_IsGenerationSafe()
    {
        World world = new();
        Entity first = world.Create();
        world.Destroy(first);
        Entity recycled = world.Create();
        TestAssert.True(recycled.Id == first.Id && recycled.Generation != first.Generation, "destroyed IDs recycle with a new generation");
        TestAssert.True(!world.IsAlive(first) && world.IsAlive(recycled), "stale entity is rejected after recycle");
    }

    private static void ComponentOperations_ReadWriteAndRemove()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(7));
        TestAssert.True(world.Has<ValueComponent>(entity), "component is present after add");
        TestAssert.True(world.TryGet(entity, out ValueComponent value) && value.Value == 7, "component can be read");
        world.Get<ValueComponent>(entity).Value = 9;
        world.Remove<ValueComponent>(entity);
        TestAssert.True(!world.Has<ValueComponent>(entity) && !world.TryGet(entity, out ValueComponent _), "component is removed");
    }

    private static void AddExisting_UpdatesWithoutDuplicate()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(1));
        world.Add(entity, new ValueComponent(2));
        ComponentStore<ValueComponent> store = new();
        store.Add(entity, new ValueComponent(1));
        store.Add(entity, new ValueComponent(2));
        TestAssert.True(store.Count == 1 && store.Get(entity).Value == 2, "adding an existing component updates one dense row");
    }

    private static void Remove_SwapKeepsMovedComponent()
    {
        World world = new();
        Entity first = world.Create();
        Entity second = world.Create();
        Entity third = world.Create();
        world.Add(first, new ValueComponent(1));
        world.Add(second, new ValueComponent(2));
        world.Add(third, new ValueComponent(3));
        world.Remove<ValueComponent>(second);
        TestAssert.True(!world.Has<ValueComponent>(second) && world.Get<ValueComponent>(third).Value == 3, "swap-remove repairs the moved entity lookup");
    }

    private static void Destroy_RemovesEveryComponent()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(1));
        world.Add(entity, new OtherComponent(2));
        world.Destroy(entity);
        TestAssert.True(!world.Has<ValueComponent>(entity) && !world.Has<OtherComponent>(entity), "destroy removes all components");
    }

    private static void MultipleTypes_UseIndependentStores()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(4));
        world.Add(entity, new OtherComponent(8));
        TestAssert.True(world.Get<ValueComponent>(entity).Value == 4 && world.Get<OtherComponent>(entity).Value == 8, "component types use independent stores");
    }

    private static void CapacityGrowth_PreservesSparseLookups()
    {
        World world = new();
        Entity[] entities = new Entity[1024];
        for (int i = 0; i < entities.Length; i++)
        {
            entities[i] = world.Create();
            world.Add(entities[i], new ValueComponent(i));
        }
        TestAssert.True(world.Get<ValueComponent>(entities[0]).Value == 0 && world.Get<ValueComponent>(entities[^1]).Value == 1023, "capacity growth preserves sparse lookups");
    }

    private static void DefaultEntity_IsRejected()
    {
        World world = new();
        Entity invalid = default;
        TestAssert.True(!world.IsAlive(invalid) && !world.Has<ValueComponent>(invalid), "default entity is invalid");
        bool threw = false;
        try { world.Add(invalid, new ValueComponent(1)); } catch (ArgumentException) { threw = true; }
        TestAssert.True(threw, "invalid entity add is rejected");
    }

    private static void SteadyStateLookup_IsAllocationFree()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(3));
        _ = world.Get<ValueComponent>(entity);
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10_000; i++) _ = world.Get<ValueComponent>(entity).Value;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        TestAssert.True(allocated == 0, $"steady-state sparse lookup allocated {allocated} bytes");
    }

    private static void Queries_CountAndIterateIntersections()
    {
        World world = new();
        Entity first = world.Create();
        Entity second = world.Create();
        Entity third = world.Create();
        world.Add(first, new ValueComponent(1));
        world.Add(first, new OtherComponent(1));
        world.Add(first, new ThirdComponent(1));
        world.Add(second, new ValueComponent(2));
        world.Add(second, new OtherComponent(2));
        world.Add(third, new ValueComponent(3));
        CountPair pair = new();
        world.Query<ValueComponent, OtherComponent>().ForEach(ref pair);
        CountTriple triple = new();
        world.Query<ValueComponent, OtherComponent, ThirdComponent>().ForEach(ref triple);
        TestAssert.True(world.Query<ValueComponent>().Count == 3 && pair.Count == 2 && triple.Count == 1, "queries count and iterate intersections");
    }

    private static void QueryRefMutation_UpdatesComponents()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(4));
        world.Add(entity, new OtherComponent(5));
        IncrementPair action = new();
        world.Query<ValueComponent, OtherComponent>().ForEach(ref action);
        TestAssert.True(world.Get<ValueComponent>(entity).Value == 5 && world.Get<OtherComponent>(entity).Value == 6, "query callback mutates components by reference");
    }

    private static void QueryIteration_IsAllocationFree()
    {
        World world = new();
        for (int i = 0; i < 256; i++)
        {
            Entity entity = world.Create();
            world.Add(entity, new ValueComponent(i));
        }
        SumValues action = new();
        Query<ValueComponent> query = world.Query<ValueComponent>();
        query.ForEach(ref action);
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) query.ForEach(ref action);
        TestAssert.True(GC.GetAllocatedBytesForCurrentThread() - start == 0, "query iteration is allocation-free after warm-up");
    }

    private static void Query_ReflectsStructuralChanges()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(1));
        Query<ValueComponent, OtherComponent> query = world.Query<ValueComponent, OtherComponent>();
        TestAssert.True(query.Count == 0, "missing secondary components are skipped");
        world.Add(entity, new OtherComponent(2));
        TestAssert.True(query.Count == 1, "adding a secondary component updates the query");
        world.Remove<OtherComponent>(entity);
        TestAssert.True(query.Count == 0, "removing a secondary component updates the query");
        world.Add(entity, new OtherComponent(2));
        world.Destroy(entity);
        TestAssert.True(query.Count == 0, "destroyed entities disappear from retained queries");
    }

    private struct ValueComponent(int value) { public int Value = value; }
    private struct OtherComponent(int value) { public int Value = value; }
    private readonly record struct ThirdComponent(int Value);

    private struct CountPair : IQueryAction<ValueComponent, OtherComponent, CountPair>
    {
        public int Count;
        public static void Execute(ref CountPair action, Entity entity, ref ValueComponent first, ref OtherComponent second) => action.Count++;
    }

    private struct CountTriple : IQueryAction<ValueComponent, OtherComponent, ThirdComponent, CountTriple>
    {
        public int Count;
        public static void Execute(ref CountTriple action, Entity entity, ref ValueComponent first, ref OtherComponent second, ref ThirdComponent third) => action.Count++;
    }

    private struct IncrementPair : IQueryAction<ValueComponent, OtherComponent, IncrementPair>
    {
        public static void Execute(ref IncrementPair action, Entity entity, ref ValueComponent first, ref OtherComponent second)
        {
            first.Value++;
            second.Value++;
        }
    }

    private struct SumValues : IQueryAction<ValueComponent, SumValues>
    {
        public int Sum;
        public static void Execute(ref SumValues action, Entity entity, ref ValueComponent component) => action.Sum += component.Value;
    }
}
