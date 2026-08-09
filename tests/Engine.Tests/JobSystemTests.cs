using Engine.Threading;

namespace Engine.Tests;

internal static class JobSystemTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(DrainAsync_DrainsAllScheduledJobs), DrainAsync_DrainsAllScheduledJobs),
        new(nameof(DrainAsync_DrainsManyJobsAcrossWorkers), DrainAsync_DrainsManyJobsAcrossWorkers),
        new(nameof(Schedule_WithDependency_RunsInOrder), Schedule_WithDependency_RunsInOrder),
        new(nameof(Schedule_DependentWaitsForBlockingDependency), Schedule_DependentWaitsForBlockingDependency),
        new(nameof(Schedule_WithMultipleDependencies_WaitsForAll), Schedule_WithMultipleDependencies_WaitsForAll),
        new(nameof(Schedule_ChainedDependencies_RunInOrder), Schedule_ChainedDependencies_RunInOrder),
        new(nameof(Schedule_DependencyAlreadyCompleted_Runs), Schedule_DependencyAlreadyCompleted_Runs),
        new(nameof(ScheduleFor_CoversRangeExactlyOnce), ScheduleFor_CoversRangeExactlyOnce),
        new(nameof(ScheduleFor_ReusedAcrossManyDispatches), ScheduleFor_ReusedAcrossManyDispatches),
        new(nameof(Complete_RethrowsJobException), Complete_RethrowsJobException),
        new(nameof(ScheduleFor_ChunkExceptionReachesBarrier), ScheduleFor_ChunkExceptionReachesBarrier),
    ];

    private static void DrainAsync_DrainsAllScheduledJobs()
    {
        int completed = 0;
        using (JobSystem jobs = new(4))
        {
            for (int i = 0; i < 100; i++) _ = jobs.Schedule(() => Interlocked.Increment(ref completed));
            jobs.DrainAsync().GetAwaiter().GetResult();
        }
        TestAssert.True(completed == 100, "job system drains all scheduled jobs");
    }

    private static void DrainAsync_DrainsManyJobsAcrossWorkers()
    {
        int completed = 0;
        using (JobSystem jobs = new(8))
        {
            for (int i = 0; i < 2000; i++) _ = jobs.Schedule(() => Interlocked.Increment(ref completed));
            jobs.DrainAsync().GetAwaiter().GetResult();
        }
        TestAssert.True(completed == 2000, "job system drains 2000 jobs across 8 workers");
    }

    private static void Schedule_WithDependency_RunsInOrder()
    {
        List<int> order = new();
        using (JobSystem jobs = new(4))
        {
            JobHandle first = jobs.Schedule(() => order.Add(1));
            JobHandle second = jobs.Schedule(() => order.Add(2), first);
            jobs.Complete(second);
        }
        TestAssert.True(order.Count == 2 && order[0] == 1 && order[1] == 2, "dependent job runs after its dependency");
    }

    private static void Schedule_DependentWaitsForBlockingDependency()
    {
        using ManualResetEventSlim gate = new(false);
        using (JobSystem jobs = new(4))
        {
            JobHandle blocker = jobs.Schedule(() => gate.Wait());
            int ran = 0;
            JobHandle dependent = jobs.Schedule(() => Interlocked.Increment(ref ran), blocker);
            Thread.Sleep(50);
            TestAssert.True(!jobs.IsComplete(dependent) && ran == 0, "dependent job stays blocked while its dependency is blocked");
            gate.Set();
            jobs.Complete(dependent);
            TestAssert.True(ran == 1, "dependent job runs once its dependency completes");
        }
    }

    private static void Schedule_WithMultipleDependencies_WaitsForAll()
    {
        int firstDone = 0;
        int secondDone = 0;
        int observed = 0;
        using (JobSystem jobs = new(4))
        {
            JobHandle first = jobs.Schedule(() => Interlocked.Increment(ref firstDone));
            JobHandle second = jobs.Schedule(() => Interlocked.Increment(ref secondDone));
            JobHandle both = jobs.Schedule(
                () => observed = Volatile.Read(ref firstDone) + Volatile.Read(ref secondDone),
                [first, second]);
            jobs.Complete(both);
        }
        TestAssert.True(observed == 2, "job with two dependencies observes both completed");
    }

    private static void Schedule_ChainedDependencies_RunInOrder()
    {
        List<int> order = new();
        using (JobSystem jobs = new(4))
        {
            JobHandle a = jobs.Schedule(() => order.Add(1));
            JobHandle b = jobs.Schedule(() => order.Add(2), a);
            JobHandle c = jobs.Schedule(() => order.Add(3), b);
            jobs.Complete(c);
        }
        TestAssert.True(order.Count == 3 && order[0] == 1 && order[1] == 2 && order[2] == 3, "chained jobs run in dependency order");
    }

    private static void Schedule_DependencyAlreadyCompleted_Runs()
    {
        int ran = 0;
        using (JobSystem jobs = new(4))
        {
            JobHandle first = jobs.Schedule(() => { });
            jobs.Complete(first);
            JobHandle second = jobs.Schedule(() => Interlocked.Increment(ref ran), first);
            jobs.Complete(second);
        }
        TestAssert.True(ran == 1, "job scheduled after its dependency completed still runs");
    }

    private static void ScheduleFor_CoversRangeExactlyOnce()
    {
        int[] counts = new int[4096];
        using (JobSystem jobs = new(8))
        {
            JobHandle barrier = jobs.ScheduleFor(counts.Length, 3, (lo, hi) =>
            {
                for (int i = lo; i < hi; i++) Interlocked.Increment(ref counts[i]);
            });
            jobs.Complete(barrier);
        }
        int total = 0;
        bool exactlyOnce = true;
        for (int i = 0; i < counts.Length; i++)
        {
            total += counts[i];
            if (counts[i] != 1) exactlyOnce = false;
        }
        TestAssert.True(exactlyOnce && total == counts.Length, "ScheduleFor covers every index exactly once");
    }

    private static void ScheduleFor_ReusedAcrossManyDispatches()
    {
        int total = 0;
        using (JobSystem jobs = new(4))
        {
            for (int dispatch = 0; dispatch < 2000; dispatch++)
            {
                JobHandle barrier = jobs.ScheduleFor(500, 10, (lo, hi) =>
                {
                    for (int i = lo; i < hi; i++) Interlocked.Increment(ref total);
                });
                jobs.Complete(barrier);
            }
        }
        TestAssert.True(total == 2000 * 500, "ScheduleFor stays correct across 2000 dispatches (slot reuse)");
    }

    private static void Complete_RethrowsJobException()
    {
        using (JobSystem jobs = new(2))
        {
            JobHandle failing = jobs.Schedule(() => throw new InvalidOperationException("boom"));
            bool threw = false;
            try { jobs.Complete(failing); }
            catch (AggregateException ex) { threw = ex.InnerException is InvalidOperationException; }
            TestAssert.True(threw, "Complete rethrows the job exception as AggregateException");
        }
    }

    private static void ScheduleFor_ChunkExceptionReachesBarrier()
    {
        using (JobSystem jobs = new(4))
        {
            JobHandle barrier = jobs.ScheduleFor(64, 1, (lo, hi) =>
            {
                if (lo == 0) throw new InvalidOperationException("chunk failed");
            });
            bool threw = false;
            try { jobs.Complete(barrier); }
            catch (AggregateException ex) { threw = ex.InnerException is InvalidOperationException; }
            TestAssert.True(threw, "chunk exception propagates to the ScheduleFor barrier");
        }
    }
}
