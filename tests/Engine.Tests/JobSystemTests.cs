using Engine.Threading;

namespace Engine.Tests;

internal static class JobSystemTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Run_WaitsForIndependentJobs), Run_WaitsForIndependentJobs),
        new(nameof(Run_ReusedAcrossManyDispatches), Run_ReusedAcrossManyDispatches),
        new(nameof(ParallelFor_CoversRangeExactlyOnce), ParallelFor_CoversRangeExactlyOnce),
        new(nameof(Wait_RethrowsJobException), Wait_RethrowsJobException),
        new(nameof(ParallelFor_ChunkExceptionReachesBarrier), ParallelFor_ChunkExceptionReachesBarrier),
        new(nameof(Run_RejectsOutstandingCapacityOverflow), Run_RejectsOutstandingCapacityOverflow),
    ];

    private static void Run_WaitsForIndependentJobs()
    {
        int completed = 0;
        using (JobSystem jobs = new(4))
        {
            JobHandle[] handles = new JobHandle[100];
            for (int i = 0; i < handles.Length; i++) handles[i] = jobs.Run(() => Interlocked.Increment(ref completed));
            for (int i = 0; i < handles.Length; i++) jobs.Wait(handles[i]);
        }
        TestAssert.True(completed == 100, "job system drains all scheduled jobs");
    }

    private static void Run_ReusedAcrossManyDispatches()
    {
        int completed = 0;
        using (JobSystem jobs = new(8))
        {
            for (int i = 0; i < 2000; i++)
            {
                JobHandle handle = jobs.Run(() => Interlocked.Increment(ref completed));
                jobs.Wait(handle);
            }
        }
        TestAssert.True(completed == 2000, "job system drains 2000 jobs across 8 workers");
    }

    private static void ParallelFor_CoversRangeExactlyOnce()
    {
        int[] counts = new int[4096];
        using (JobSystem jobs = new(8))
        {
            JobHandle barrier = jobs.ParallelFor(counts.Length, 3, (lo, hi) =>
            {
                for (int i = lo; i < hi; i++) Interlocked.Increment(ref counts[i]);
            });
            jobs.Wait(barrier);
        }
        int total = 0;
        bool exactlyOnce = true;
        for (int i = 0; i < counts.Length; i++)
        {
            total += counts[i];
            if (counts[i] != 1) exactlyOnce = false;
        }
        TestAssert.True(exactlyOnce && total == counts.Length, "ParallelFor covers every index exactly once");
    }

    private static void ParallelFor_ReusedAcrossManyDispatches()
    {
        int total = 0;
        using (JobSystem jobs = new(4))
        {
            for (int dispatch = 0; dispatch < 2000; dispatch++)
            {
                JobHandle barrier = jobs.ParallelFor(500, 10, (lo, hi) =>
                {
                    for (int i = lo; i < hi; i++) Interlocked.Increment(ref total);
                });
                jobs.Wait(barrier);
            }
        }
        TestAssert.True(total == 2000 * 500, "ParallelFor stays correct across 2000 dispatches (slot reuse)");
    }

    private static void Wait_RethrowsJobException()
    {
        using (JobSystem jobs = new(2))
        {
            JobHandle failing = jobs.Run(() => throw new InvalidOperationException("boom"));
            bool threw = false;
            try { jobs.Wait(failing); }
            catch (AggregateException ex) { threw = ex.InnerException is InvalidOperationException; }
            TestAssert.True(threw, "Complete rethrows the job exception as AggregateException");
        }
    }

    private static void ParallelFor_ChunkExceptionReachesBarrier()
    {
        using (JobSystem jobs = new(4))
        {
            JobHandle barrier = jobs.ParallelFor(64, 1, (lo, hi) =>
            {
                if (lo == 0) throw new InvalidOperationException("chunk failed");
            });
            bool threw = false;
            try { jobs.Wait(barrier); }
            catch (AggregateException ex) { threw = ex.InnerException is InvalidOperationException; }
            TestAssert.True(threw, "chunk exception propagates to the ParallelFor barrier");
        }
    }

    private static void Run_RejectsOutstandingCapacityOverflow()
    {
        using ManualResetEventSlim gate = new(false);
        using JobSystem jobs = new(1);
        jobs.Run(() => gate.Wait());
        for (int i = 0; i < 4095; i++) jobs.Run(static () => { });
        bool threw = false;
        try { jobs.Run(static () => { }); }
        catch (InvalidOperationException) { threw = true; }
        gate.Set();
        TestAssert.True(threw, "job system rejects more than 4096 outstanding jobs");
    }
}
