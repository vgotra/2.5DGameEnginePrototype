using Engine.Threading;

namespace Engine.Tests;

internal static class JobSystemTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(DrainAsync_DrainsAllScheduledJobs), DrainAsync_DrainsAllScheduledJobs),
        new(nameof(DrainAsync_DrainsManyJobsAcrossWorkers), DrainAsync_DrainsManyJobsAcrossWorkers),
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
}
