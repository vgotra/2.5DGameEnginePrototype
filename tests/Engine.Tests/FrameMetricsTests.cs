using Engine.App;
using Engine.Rendering;

namespace Engine.Tests;

internal static class FrameMetricsTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Statistics_ReportAverageFpsAndPercentiles), Statistics_ReportAverageFpsAndPercentiles),
        new(nameof(Statistics_HandleJitterAndLongFrame), Statistics_HandleJitterAndLongFrame),
        new(nameof(Statistics_WarmedUpdatesAreAllocationFree), Statistics_WarmedUpdatesAreAllocationFree),
        new(nameof(PresentationDiagnostics_PreservesRequestedAndSelectedModes), PresentationDiagnostics_PreservesRequestedAndSelectedModes),
    ];

    private static void Statistics_ReportAverageFpsAndPercentiles()
    {
        FrameMetrics metrics = new(60, "Mailbox");
        for (int i = 0; i < 120; i++) metrics.Add(16.6667, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        FrameStatistics statistics = metrics.LastStatistics;
        TestAssert.True(Math.Abs(statistics.AverageFps - 60) < 0.1, "average FPS is calculated from complete frame time");
        TestAssert.True(Math.Abs(statistics.P95FrameMs - 16.6667) < 0.01 && Math.Abs(statistics.P99FrameMs - 16.6667) < 0.01, "percentiles report steady frame time");
    }

    private static void Statistics_HandleJitterAndLongFrame()
    {
        FrameMetrics metrics = new(0, "unknown");
        for (int i = 0; i < 120; i++) metrics.Add(16, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        for (int i = 0; i < 119; i++) metrics.Add(i == 118 ? 100 : 16, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        TestAssert.True(metrics.LastStatistics.MaxFrameMs == 16, "statistics remain in the previous window until reset");
        metrics.Add(16, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        TestAssert.True(metrics.LastStatistics.MaxFrameMs == 100, "long frame is included in maximum");
    }

    private static void Statistics_WarmedUpdatesAreAllocationFree()
    {
        FrameMetrics metrics = new(0, "unknown");
        for (int i = 0; i < 120; i++) metrics.Add(16, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 119; i++) metrics.Add(16, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        TestAssert.True(GC.GetAllocatedBytesForCurrentThread() == before, "warmed frame metric updates allocate no memory");
    }

    private static void PresentationDiagnostics_PreservesRequestedAndSelectedModes()
    {
        PresentationDiagnostics diagnostics = new(PresentMode.Mailbox, PresentMode.Fifo, true, 3);
        TestAssert.True(diagnostics.RequestedMode == PresentMode.Mailbox && diagnostics.SelectedMode == PresentMode.Fifo && diagnostics.UsedFallback && diagnostics.SwapchainImageCount == 3, "presentation diagnostics preserve fallback state");
    }
}
