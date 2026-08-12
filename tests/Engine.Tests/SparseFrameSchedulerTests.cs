using Engine.Ecs.Sparse;
using Engine.Threading;

namespace Engine.Tests;

internal static class SparseFrameSchedulerTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(SerialSystems_RunInRegistrationOrder), SerialSystems_RunInRegistrationOrder),
        new(nameof(EntityCommands_DeferredStructuralMutations), EntityCommands_DeferredStructuralMutations),
        new(nameof(EntityCommands_ReservedCreateAndFifoApply), EntityCommands_ReservedCreateAndFifoApply),
        new(nameof(EntityCommands_ReservedDestroyIsHarmless), EntityCommands_ReservedDestroyIsHarmless),
        new(nameof(PolicyDiagnostics_ReportDecisionWithoutChangingOrder), PolicyDiagnostics_ReportDecisionWithoutChangingOrder),
        new(nameof(BackgroundSystems_AreSkipped), BackgroundSystems_AreSkipped),
    ];

    private static void SerialSystems_RunInRegistrationOrder()
    {
        using JobSystem jobs = new(1);
        FrameScheduler scheduler = new(jobs);
        List<int> order = new();
        scheduler.Register(new RecordingSystem(order, 1));
        scheduler.Register(new RecordingSystem(order, 2));
        scheduler.Run(new World(), 0.016f);
        TestAssert.True(order.SequenceEqual([1, 2]), "sparse frame systems run deterministically");
    }

    private static void EntityCommands_DeferredStructuralMutations()
    {
        World world = new();
        Entity entity = world.Create();
        world.Add(entity, new ValueComponent(7));
        EntityCommands buffer = new();
        buffer.Remove<ValueComponent>(entity);
        buffer.Add(entity, new ValueComponent(9));
        buffer.Destroy(entity);
        TestAssert.True(world.IsAlive(entity), "destroy remains deferred before apply");
        buffer.Apply(world);
        TestAssert.True(!world.IsAlive(entity) && !world.Has<ValueComponent>(entity), "deferred destroy removes the entity and components");
    }

    private static void EntityCommands_ReservedCreateAndFifoApply()
    {
        World world = new();
        EntityCommands buffer = new();
        Entity entity = buffer.Create(world);
        buffer.Add(entity, new ValueComponent(11));
        TestAssert.True(!world.IsAlive(entity) && !world.Has<ValueComponent>(entity), "reserved entity remains hidden before apply");
        buffer.Apply(world);
        TestAssert.True(world.IsAlive(entity), "reserved entity activates during apply");
        TestAssert.True(world.Get<ValueComponent>(entity).Value == 11, "create and add preserve FIFO order");
        buffer.Clear();
        buffer.Remove<ValueComponent>(entity);
        buffer.Apply(world);
        TestAssert.True(!world.Has<ValueComponent>(entity), "remove applies to an existing entity");
    }

    private static void EntityCommands_ReservedDestroyIsHarmless()
    {
        World world = new();
        EntityCommands buffer = new();
        Entity entity = buffer.Create(world);
        buffer.Destroy(entity);
        buffer.Apply(world);
        TestAssert.True(!world.IsAlive(entity) && !world.Entities.IsReserved(entity), "destroy cancels a reserved entity");
        buffer.Clear();
        buffer.Destroy(entity);
        buffer.Remove<ValueComponent>(entity);
        buffer.Apply(world);
        TestAssert.True(!world.IsAlive(entity), "stale destroy and remove are harmless");
    }

    private static void PolicyDiagnostics_ReportDecisionWithoutChangingOrder()
    {
        using JobSystem jobs = new(1);
        FrameScheduler scheduler = new(jobs) { DiagnosticsEnabled = true };
        RecordingSystem system = new([], 1);
        scheduler.Register(system, new("AI", ExecutionPolicy.Adaptive, 1, true, true, false));
        World world = new();
        world.Create();
        scheduler.Run(world, 0.016f);
        SystemDiagnostic diagnostic = scheduler.Diagnostics[0];
        TestAssert.True(diagnostic.Name == "AI" && diagnostic.ParallelSelected && diagnostic.ItemCount == 1, "policy diagnostics report adaptive selection");
        TestAssert.True(diagnostic.AllocatedBytes >= 0, "policy diagnostics report allocations");
    }

    private static void BackgroundSystems_AreSkipped()
    {
        using JobSystem jobs = new(1);
        FrameScheduler scheduler = new(jobs) { DiagnosticsEnabled = true };
        List<int> order = new();
        scheduler.Register(new RecordingSystem(order, 1), new("AssetLoading", ExecutionPolicy.Background, 0, false, false, true));
        scheduler.Run(new World(), 0.016f);
        TestAssert.True(order.Count == 0, "background systems do not run in the fixed-step scheduler");
    }

    private struct ValueComponent(int value)
    {
        public int Value = value;
    }

    private sealed class RecordingSystem(List<int> order, int value) : ISystem
    {
        public void Update(World world, float deltaSeconds) => order.Add(value);
    }
}
