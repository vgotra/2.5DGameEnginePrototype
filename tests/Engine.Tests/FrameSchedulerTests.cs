using Engine.Ecs;
using Engine.Threading;

namespace Engine.Tests;

internal static class FrameSchedulerTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(StagesAndRegistrations_RunDeterministically), StagesAndRegistrations_RunDeterministically),
        new(nameof(ParallelGroup_RejectsConflictingSystems), ParallelGroup_RejectsConflictingSystems),
        new(nameof(ParallelGroup_CompletesBeforeNextStage), ParallelGroup_CompletesBeforeNextStage),
        new(nameof(LateRegistration_RebuildsPlan), LateRegistration_RebuildsPlan),
        new(nameof(EmptySchedule_IsValid), EmptySchedule_IsValid),
    ];

    private static void StagesAndRegistrations_RunDeterministically()
    {
        using JobSystem jobs = new(2);
        FrameScheduler scheduler = new(jobs);
        FrameStage update = scheduler.AddStage("update");
        FrameStage render = scheduler.AddStage("render");
        List<int> order = new();
        scheduler.Register("first", update, new RecordingSystem(order, 1));
        scheduler.Register("second", update, new RecordingSystem(order, 2));
        scheduler.Register("third", render, new RecordingSystem(order, 3));
        scheduler.BuildPlan();
        scheduler.Run(new World(), 0.016f);
        TestAssert.True(order.SequenceEqual([1, 2, 3]), "explicit stages and registration order are deterministic");
        TestAssert.True(scheduler.Plan.Count == 3, "built plan exposes registrations");
    }

    private static void ParallelGroup_RejectsConflictingSystems()
    {
        using JobSystem jobs = new(2);
        FrameScheduler scheduler = new(jobs);
        FrameStage stage = scheduler.AddStage("simulation");
        ParallelGroup group = scheduler.CreateParallelGroup("independent");
        scheduler.Register("writer", stage, new AccessSystem(ComponentAccess.Write<int>()), group);
        bool rejected = false;
        try { scheduler.Register("reader", stage, new AccessSystem(ComponentAccess.Read<int>()), group); }
        catch (InvalidOperationException) { rejected = true; }
        TestAssert.True(rejected, "conflicting systems are rejected from a parallel group");
    }

    private static void ParallelGroup_CompletesBeforeNextStage()
    {
        using JobSystem jobs = new(2);
        FrameScheduler scheduler = new(jobs);
        FrameStage parallel = scheduler.AddStage("parallel");
        FrameStage next = scheduler.AddStage("next");
        ParallelGroup group = scheduler.CreateParallelGroup("independent");
        int completed = 0;
        scheduler.Register("one", parallel, new CallbackSystem(() => Interlocked.Increment(ref completed)), group);
        scheduler.Register("two", parallel, new CallbackSystem(() => Interlocked.Increment(ref completed)), group);
        scheduler.Register("after", next, new CallbackSystem(() => TestAssert.True(completed == 2, "next stage waits for parallel group")));
        scheduler.Run(new World(), 0.016f);
    }

    private static void LateRegistration_RebuildsPlan()
    {
        using JobSystem jobs = new(1);
        FrameScheduler scheduler = new(jobs);
        FrameStage stage = scheduler.AddStage("update");
        scheduler.Register("first", stage, new AccessSystem(ComponentAccess.Read<int>()));
        _ = scheduler.Plan;
        scheduler.Register("second", stage, new AccessSystem(ComponentAccess.Read<int>()));
        TestAssert.True(scheduler.Plan.Count == 2, "late registration rebuilds the plan");
    }

    private static void EmptySchedule_IsValid()
    {
        using JobSystem jobs = new(1);
        FrameScheduler scheduler = new(jobs);
        scheduler.AddStage("empty");
        scheduler.Run(new World(), 0.016f);
        Engine.Ecs.Barrier barrier = scheduler.ScheduleBarrier();
        barrier.Complete();
        TestAssert.True(barrier.IsComplete, "explicit barrier completes");
    }

    private sealed class RecordingSystem(List<int> order, int value) : ISystem
    {
        public ComponentAccess Access => ComponentAccess.Read<int>();
        public void Update(World world, float deltaSeconds) => order.Add(value);
    }

    private sealed class AccessSystem(ComponentAccess access) : ISystem
    {
        public ComponentAccess Access => access;
        public void Update(World world, float deltaSeconds) { }
    }

    private sealed class CallbackSystem(Action callback) : ISystem
    {
        public ComponentAccess Access => ComponentAccess.Read<int>();
        public void Update(World world, float deltaSeconds) => callback();
    }
}
