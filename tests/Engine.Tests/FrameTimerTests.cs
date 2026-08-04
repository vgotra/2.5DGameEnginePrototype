using System.Diagnostics;
using Engine.Core;

namespace Engine.Tests;

internal static class FrameTimerTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Advance_UncappedTimerReturnsNonNegative), Advance_UncappedTimerReturnsNonNegative),
        new(nameof(WaitForNextFrame_UncappedReturnsImmediately), WaitForNextFrame_UncappedReturnsImmediately),
        new(nameof(WaitForNextFrame_CappedPacesToTarget), WaitForNextFrame_CappedPacesToTarget),
    ];

    private static void Advance_UncappedTimerReturnsNonNegative()
    {
        FrameTimer timer = new();
        TestAssert.True(timer.Advance() >= 0, "uncapped frame timer advances");
    }

    private static void WaitForNextFrame_UncappedReturnsImmediately()
    {
        FrameTimer timer = new();
        Stopwatch sw = Stopwatch.StartNew();
        timer.WaitForNextFrame();
        sw.Stop();
        TestAssert.True(sw.ElapsedMilliseconds < 5, "uncapped wait returns immediately");
    }

    private static void WaitForNextFrame_CappedPacesToTarget()
    {
        FrameTimer timer = new(60);
        timer.Advance();
        Stopwatch sw = Stopwatch.StartNew();
        timer.WaitForNextFrame();
        sw.Stop();
        TestAssert.True(sw.ElapsedMilliseconds is >= 8 and <= 250, "frame cap paces to ~16.7ms");
    }
}
