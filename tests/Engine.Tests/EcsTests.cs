using Engine.Core;
using Engine.Ecs;
using Engine.Threading;

namespace Engine.Tests;

internal static class EcsTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Create_EntityIsAlive), Create_EntityIsAlive),
        new(nameof(AddThenTryGet_ReturnsStoredValue), AddThenTryGet_ReturnsStoredValue),
        new(nameof(Destroy_StaleEntityIsRejected), Destroy_StaleEntityIsRejected),
        new(nameof(Destroy_PurgesComponentStorage), Destroy_PurgesComponentStorage),
        new(nameof(Create_RecyclesEntityIndex), Create_RecyclesEntityIndex),
        new(nameof(Add_VisibleAfterIndexRecycle), Add_VisibleAfterIndexRecycle),
        new(nameof(Add_StaleGenerationRejectedAfterRecycle), Add_StaleGenerationRejectedAfterRecycle),
        new(nameof(Destroy_PurgesEveryComponentStore), Destroy_PurgesEveryComponentStore),
        new(nameof(RemoveComponent_MovesEntityBackToEmptyArchetype), RemoveComponent_MovesEntityBackToEmptyArchetype),
        new(nameof(SetComponent_OverwritesAndGetMutatesStorage), SetComponent_OverwritesAndGetMutatesStorage),
        new(nameof(HasComponent_TracksAddRemove), HasComponent_TracksAddRemove),
        new(nameof(Query_Count_SumsAcrossArchetypes), Query_Count_SumsAcrossArchetypes),
        new(nameof(Query_ForEach_SingleComponentVisitsAllMatches), Query_ForEach_SingleComponentVisitsAllMatches),
        new(nameof(Query_ForEach_TwoComponentMatchesAcrossArchetypes), Query_ForEach_TwoComponentMatchesAcrossArchetypes),
        new(nameof(Query_ForEach_ThreeComponentMatches), Query_ForEach_ThreeComponentMatches),
        new(nameof(Query_ForEachParallel_MatchesSerialSingleComponent), Query_ForEachParallel_MatchesSerialSingleComponent),
        new(nameof(Query_ForEachParallel_MatchesSerialTwoComponent), Query_ForEachParallel_MatchesSerialTwoComponent),
        new(nameof(Query_ForEachParallel_DeterministicAcrossRuns), Query_ForEachParallel_DeterministicAcrossRuns),
        new(nameof(CommandBuffer_AppliesAddsRemovesAndDestroys), CommandBuffer_AppliesAddsRemovesAndDestroys),
        new(nameof(CommandBuffer_ClearDiscardsBufferedMutations), CommandBuffer_ClearDiscardsBufferedMutations),
        new(nameof(Scheduler_RunsSystemsInOrderWhenNoConflicts), Scheduler_RunsSystemsInOrderWhenNoConflicts),
        new(nameof(Scheduler_NewSystemRunsAfterExistingConflictingSystem), Scheduler_NewSystemRunsAfterExistingConflictingSystem),
        new(nameof(Scheduler_ReadReadDoesNotConflict), Scheduler_ReadReadDoesNotConflict),
        new(nameof(Scheduler_RebuildsOrderAfterLateRegister), Scheduler_RebuildsOrderAfterLateRegister),
        new(nameof(RecycleAndMove_StressKeepsStorageIsolated), RecycleAndMove_StressKeepsStorageIsolated),
        new(nameof(Query_ForEach_ZeroAllocations), Query_ForEach_ZeroAllocations),
    ];

    private static void Create_EntityIsAlive()
    {
        World world = new();
        EntityId entity = world.Create();
        TestAssert.True(world.IsAlive(entity), "entity is alive after create");
    }

    private static void AddThenTryGet_ReturnsStoredValue()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new TestComponent(42));
        TestAssert.True(world.TryGetComponent(entity, out TestComponent value) && value.Value == 42, "component value is stored and readable");
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
        world.AddComponent(entity, new TestComponent(42));
        world.Destroy(entity);
        TestAssert.True(world.Query<TestComponent>().Count == 0, "destroy purges component storage");
    }

    private static void Create_RecyclesEntityIndex()
    {
        World world = new();
        EntityId first = world.Create();
        world.Destroy(first);
        EntityId recycled = world.Create();
        TestAssert.True(recycled.Index == first.Index, "entity index is recycled after destroy");
    }

    private static void Add_VisibleAfterIndexRecycle()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Destroy(entity);
        EntityId recycled = world.Create();
        world.AddComponent(recycled, new TestComponent(7));
        TestAssert.True(world.TryGetComponent(recycled, out TestComponent value) && value.Value == 7, "component is readable after index recycle");
    }

    private static void Add_StaleGenerationRejectedAfterRecycle()
    {
        World world = new();
        EntityId entity = world.Create();
        world.Destroy(entity);
        EntityId recycled = world.Create();
        world.AddComponent(recycled, new TestComponent(7));
        TestAssert.True(!world.TryGetComponent(entity, out TestComponent _), "stale generation is rejected after index recycle");
    }

    private static void Destroy_PurgesEveryComponentStore()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new TestComponent(1));
        world.AddComponent(entity, new TestComponent2(2));
        TestAssert.True(world.Query<TestComponent>().Count == 1 && world.Query<TestComponent2>().Count == 1, "two component stores are populated");
        world.Destroy(entity);
        TestAssert.True(world.Query<TestComponent>().Count == 0 && world.Query<TestComponent2>().Count == 0, "destroy purges every component store");
    }

    private static void RemoveComponent_MovesEntityBackToEmptyArchetype()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new TestComponent(1));
        world.AddComponent(entity, new TestComponent2(2));
        world.RemoveComponent<TestComponent2>(entity);
        TestAssert.True(!world.HasComponent<TestComponent2>(entity), "removed component is gone");
        TestAssert.True(world.TryGetComponent(entity, out TestComponent value) && value.Value == 1, "sibling component survives the move");
        TestAssert.True(world.Query<TestComponent>().Count == 1 && world.Query<TestComponent, TestComponent2>().Count == 0, "entity moved out of the two-component archetype");
    }

    private static void SetComponent_OverwritesAndGetMutatesStorage()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new MutableComponent(1));
        world.SetComponent(entity, new MutableComponent(9));
        TestAssert.True(world.Get<MutableComponent>(entity).Value == 9, "SetComponent overwrites the stored value");
        world.Get<MutableComponent>(entity).Value = 5;
        TestAssert.True(world.TryGetComponent(entity, out MutableComponent value) && value.Value == 5, "Get returns a reference into component storage");
    }

    private static void HasComponent_TracksAddRemove()
    {
        World world = new();
        EntityId entity = world.Create();
        TestAssert.True(!world.HasComponent<TestComponent>(entity), "no component before add");
        world.AddComponent(entity, new TestComponent(1));
        TestAssert.True(world.HasComponent<TestComponent>(entity), "component present after add");
        world.RemoveComponent<TestComponent>(entity);
        TestAssert.True(!world.HasComponent<TestComponent>(entity), "component absent after remove");
    }

    private static void Query_Count_SumsAcrossArchetypes()
    {
        World world = new();
        EntityId one = world.Create();
        EntityId two = world.Create();
        EntityId none = world.Create();
        world.AddComponent(one, new TestComponent(1));
        world.AddComponent(two, new TestComponent(2));
        world.AddComponent(two, new TestComponent2(3));
        TestAssert.True(world.Query<TestComponent>().Count == 2, "single-component query counts both archetypes");
        TestAssert.True(world.Query<TestComponent, TestComponent2>().Count == 1, "two-component query counts the joint archetype");
        TestAssert.True(world.Query<TestComponent2>().Count == 1, "single query over the joint archetype counts one entity");
        TestAssert.True(world.IsAlive(none), "entity with no components is untouched by queries");
    }

    private static void Query_ForEach_SingleComponentVisitsAllMatches()
    {
        World world = new();
        EntityId one = world.Create();
        EntityId two = world.Create();
        world.AddComponent(one, new TestComponent(1));
        world.AddComponent(two, new TestComponent(2));
        world.AddComponent(two, new TestComponent2(3));
        CollectValues body = new();
        world.Query<TestComponent>().ForEach(ref body);
        TestAssert.True(body.Values.Count == 2 && body.Values.Contains(1) && body.Values.Contains(2), "ForEach visits every matching entity once");
    }

    private static void Query_ForEach_TwoComponentMatchesAcrossArchetypes()
    {
        World world = new();
        EntityId one = world.Create();
        EntityId two = world.Create();
        world.AddComponent(one, new TestComponent(1));
        world.AddComponent(one, new TestComponent2(10));
        world.AddComponent(two, new TestComponent2(20));
        CollectPairValues body = new();
        world.Query<TestComponent, TestComponent2>().ForEach(ref body);
        TestAssert.True(body.Count == 1, "two-component query only matches entities holding both");
    }

    private static void Query_ForEach_ThreeComponentMatches()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new TestComponent(1));
        world.AddComponent(entity, new TestComponent2(2));
        world.AddComponent(entity, new TestComponent3(3));
        VisitThreeBody body = new();
        world.Query<TestComponent, TestComponent2, TestComponent3>().ForEach(ref body);
        TestAssert.True(body.Count == 1, "three-component query visits the matching entity");
        TestAssert.True(world.Query<TestComponent, TestComponent2>().Count == 1, "subset query still matches");
    }

    private static void Query_ForEachParallel_MatchesSerialSingleComponent()
    {
        using (JobSystem jobs = new(4))
        {
            World world = new();
            const int count = 20_000;
            CreatePopulation(world, count);
            long serial = SumSingle(world, count, serial: true);
            long parallel = SumSingle(world, count, serial: false, jobs);
            TestAssert.True(serial == parallel, "parallel single-component query sums identically to serial");
        }
    }

    private static void Query_ForEachParallel_MatchesSerialTwoComponent()
    {
        using (JobSystem jobs = new(4))
        {
            World world = new();
            const int count = 20_000;
            CreatePopulation(world, count);
            long serial = SumPair(world, count, serial: true);
            long parallel = SumPair(world, count, serial: false, jobs);
            TestAssert.True(serial == parallel, "parallel two-component query sums identically to serial");
        }
    }

    private static void Query_ForEachParallel_DeterministicAcrossRuns()
    {
        using (JobSystem jobs = new(4))
        {
            World world = new();
            const int count = 20_000;
            CreatePopulation(world, count);
            long first = SumSingle(world, count, serial: false, jobs);
            long second = SumSingle(world, count, serial: false, jobs);
            TestAssert.True(first == second, "parallel query is deterministic across runs");
        }
    }

    private static void CommandBuffer_AppliesAddsRemovesAndDestroys()
    {
        World world = new();
        EntityId toAdd = world.Create();
        EntityId toRemove = world.Create();
        EntityId toDestroy = world.Create();
        world.AddComponent(toRemove, new TestComponent(9));
        WorldCommandBuffer buffer = new();
        buffer.AddComponent(toAdd, new TestComponent(5));
        buffer.RemoveComponent<TestComponent>(toRemove);
        buffer.Destroy(toDestroy);
        TestAssert.True(!world.HasComponent<TestComponent>(toAdd) && world.HasComponent<TestComponent>(toRemove) && world.IsAlive(toDestroy), "buffered mutations are not applied before Apply");
        buffer.Apply(world);
        TestAssert.True(world.TryGetComponent(toAdd, out TestComponent added) && added.Value == 5, "buffered add is applied");
        TestAssert.True(!world.HasComponent<TestComponent>(toRemove), "buffered remove is applied");
        TestAssert.True(!world.IsAlive(toDestroy), "buffered destroy is applied");
    }

    private static void CommandBuffer_ClearDiscardsBufferedMutations()
    {
        World world = new();
        EntityId entity = world.Create();
        WorldCommandBuffer buffer = new();
        buffer.AddComponent(entity, new TestComponent(7));
        buffer.Clear();
        buffer.Apply(world);
        TestAssert.True(!world.HasComponent<TestComponent>(entity), "cleared buffer applies nothing");
    }

    private static void Scheduler_RunsSystemsInOrderWhenNoConflicts()
    {
        World world = new();
        List<string> log = new();
        SystemScheduler scheduler = new();
        scheduler.Register(new RecordingSystem("A", ComponentAccess.Write<TestComponent>(), log));
        scheduler.Register(new RecordingSystem("B", ComponentAccess.Write<TestComponent2>(), log));
        scheduler.Run(world, 1f);
        TestAssert.True(log.Count == 2 && log[0] == "A" && log[1] == "B", "non-conflicting systems run in registration order");
    }

    private static void Scheduler_NewSystemRunsAfterExistingConflictingSystem()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new TestComponent(0));
        SystemScheduler scheduler = new();
        WriteSystem writer = new() { Entity = entity, Value = 42 };
        ReadSystem reader = new() { Entity = entity };
        scheduler.Register(writer);
        scheduler.Register(reader);
        scheduler.Run(world, 1f);
        TestAssert.True(reader.Observed == 42, "a newly registered reader runs after the existing writer it conflicts with");
    }

    private static void Scheduler_ReadReadDoesNotConflict()
    {
        World world = new();
        EntityId entity = world.Create();
        world.AddComponent(entity, new TestComponent(1));
        List<string> log = new();
        SystemScheduler scheduler = new();
        scheduler.Register(new RecordingSystem("A", ComponentAccess.Read<TestComponent>(), log));
        scheduler.Register(new RecordingSystem("B", ComponentAccess.Read<TestComponent>(), log));
        scheduler.Run(world, 1f);
        TestAssert.True(log.Count == 2 && log[0] == "A" && log[1] == "B", "read/read does not reorder systems");
    }

    private static void Scheduler_RebuildsOrderAfterLateRegister()
    {
        World world = new();
        List<string> log = new();
        SystemScheduler scheduler = new();
        RecordingSystem a = new("A", ComponentAccess.Write<TestComponent>(), log);
        RecordingSystem b = new("B", ComponentAccess.Write<TestComponent>(), log);
        scheduler.Register(a);
        scheduler.Run(world, 1f);
        scheduler.Register(b);
        scheduler.Run(world, 1f);
        TestAssert.True(log.Count == 3 && log[0] == "A" && log[1] == "A" && log[2] == "B", "order is rebuilt after a late register");
    }

    private static void RecycleAndMove_StressKeepsStorageIsolated()
    {
        World world = new();
        for (int round = 0; round < 200; round++)
        {
            EntityId a = world.Create();
            EntityId b = world.Create();
            world.AddComponent(a, new TestComponent(round));
            world.Destroy(b);
            EntityId c = world.Create();
            world.AddComponent(c, new TestComponent2(round));
            TestAssert.True(world.IsAlive(a) && world.IsAlive(c) && !world.IsAlive(b), "alive set is correct across recycle");
            TestAssert.True(world.TryGetComponent(a, out TestComponent va) && va.Value == round, "a keeps its component across recycles");
            TestAssert.True(world.TryGetComponent(c, out TestComponent2 vc) && vc.Value == round, "recycled entity c has its own component");
            TestAssert.True(!world.TryGetComponent(c, out TestComponent _), "recycled entity never inherits another entity's component type");
            world.Destroy(a);
        }
    }

    private static void Query_ForEach_ZeroAllocations()
    {
        World world = new();
        const int count = 100_000;
        for (int i = 0; i < count; i++)
        {
            EntityId entity = world.Create();
            world.AddComponent(entity, new TestComponent(i));
            world.AddComponent(entity, new TestComponent2(i * 3));
        }
        Query<TestComponent, TestComponent2> query = world.Query<TestComponent, TestComponent2>();
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) _ = query.Count;
        long countAlloc = GC.GetAllocatedBytesForCurrentThread() - start;

        Sum2Body body = new() { Sums = new long[count] };
        start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) query.ForEach(ref body);
        long foreachAlloc = GC.GetAllocatedBytesForCurrentThread() - start;

        TestComponent[] a = new TestComponent[count];
        TestComponent2[] b = new TestComponent2[count];
        Sum2Body directBody = new() { Sums = new long[count] };
        start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            for (int r = 0; r < count; r++)
                Sum2Body.Execute(ref directBody, new EntityId((uint)r, 0), ref a[r], ref b[r]);
        }
        long directAlloc = GC.GetAllocatedBytesForCurrentThread() - start;

        using (JobSystem jobs = new(4))
        {
            Sum2Body parallelBody = new() { Sums = new long[count] };
            start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) query.ForEachParallel(jobs, ref parallelBody, 64);
            long parallelAlloc = GC.GetAllocatedBytesForCurrentThread() - start;
            TestAssert.True(foreachAlloc == 0, $"ForEach allocated {foreachAlloc} bytes (Count={countAlloc}, Direct={directAlloc}, Parallel={parallelAlloc}) over 1000 iterations");
        }
    }

    private static void CreatePopulation(World world, int count)
    {
        for (int i = 0; i < count; i++)
        {
            EntityId entity = world.Create();
            switch (i % 4)
            {
                case 0:
                    world.AddComponent(entity, new TestComponent(i));
                    break;
                case 1:
                    world.AddComponent(entity, new TestComponent(i));
                    world.AddComponent(entity, new TestComponent2(i * 3));
                    break;
                case 2:
                    world.AddComponent(entity, new TestComponent(i));
                    world.AddComponent(entity, new TestComponent3(i * 7));
                    break;
            }
        }
    }

    private static long SumSingle(World world, int count, bool serial, JobSystem? jobs = null)
    {
        long[] sums = new long[count];
        SumBody body = new() { Sums = sums };
        if (serial) world.Query<TestComponent>().ForEach(ref body);
        else world.Query<TestComponent>().ForEachParallel(jobs!, ref body, 64);
        long total = 0;
        for (int i = 0; i < sums.Length; i++) total += sums[i];
        return total;
    }

    private static long SumPair(World world, int count, bool serial, JobSystem? jobs = null)
    {
        long[] sums = new long[count];
        Sum2Body body = new() { Sums = sums };
        if (serial) world.Query<TestComponent, TestComponent2>().ForEach(ref body);
        else world.Query<TestComponent, TestComponent2>().ForEachParallel(jobs!, ref body, 64);
        long total = 0;
        for (int i = 0; i < sums.Length; i++) total += sums[i];
        return total;
    }

    internal readonly record struct TestComponent(int Value);
    internal readonly record struct TestComponent2(int Value);
    internal readonly record struct TestComponent3(int Value);

    private struct MutableComponent
    {
        public int Value;
        public MutableComponent(int value) => Value = value;
    }

    private struct CollectValues : IForEach<TestComponent, CollectValues>
    {
        public readonly List<int> Values;
        public CollectValues() => Values = new List<int>();
        public static void Execute(ref CollectValues body, EntityId entity, ref TestComponent a) => body.Values.Add(a.Value);
    }

    private struct CollectPairValues : IForEach<TestComponent, TestComponent2, CollectPairValues>
    {
        public int Count;
        public static void Execute(ref CollectPairValues body, EntityId entity, ref TestComponent a, ref TestComponent2 b) => body.Count++;
    }

    private struct VisitThreeBody : IForEach<TestComponent, TestComponent2, TestComponent3, VisitThreeBody>
    {
        public int Count;
        public static void Execute(ref VisitThreeBody body, EntityId entity, ref TestComponent a, ref TestComponent2 b, ref TestComponent3 c) => body.Count++;
    }

    private struct SumBody : IForEach<TestComponent, SumBody>
    {
        public long[] Sums;
        public static void Execute(ref SumBody body, EntityId entity, ref TestComponent a) => body.Sums[entity.Index] = a.Value;
    }

    private struct Sum2Body : IForEach<TestComponent, TestComponent2, Sum2Body>
    {
        public long[] Sums;
        public static void Execute(ref Sum2Body body, EntityId entity, ref TestComponent a, ref TestComponent2 b)
            => body.Sums[entity.Index] = a.Value * 1_000_000L + b.Value;
    }

    private sealed class RecordingSystem : ISystem
    {
        private readonly string _name;
        private readonly List<string> _log;
        public RecordingSystem(string name, ComponentAccess access, List<string> log)
        {
            _name = name;
            Access = access;
            _log = log;
        }
        public ComponentAccess Access { get; }
        public void Update(World world, float deltaSeconds) => _log.Add(_name);
    }

    private sealed class WriteSystem : ISystem
    {
        public EntityId Entity;
        public int Value;
        public ComponentAccess Access => ComponentAccess.Write<TestComponent>();
        public void Update(World world, float deltaSeconds) => world.SetComponent(Entity, new TestComponent(Value));
    }

    private sealed class ReadSystem : ISystem
    {
        public EntityId Entity;
        public int Observed;
        public ComponentAccess Access => ComponentAccess.Read<TestComponent>();
        public void Update(World world, float deltaSeconds) => Observed = world.Get<TestComponent>(Entity).Value;
    }
}
