using Engine.Core;

namespace Engine.Tests;

internal static class GameClockTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Advance_ConsumesOneFixedStepPerAdvance), Advance_ConsumesOneFixedStepPerAdvance),
        new(nameof(Advance_ClampsLongFrames), Advance_ClampsLongFrames),
        new(nameof(Advance_AccumulatorBounded), Advance_AccumulatorBounded),
        new(nameof(InterpolationAlpha_TracksAccumulator), InterpolationAlpha_TracksAccumulator),
    ];

    private static void Advance_ConsumesOneFixedStepPerAdvance()
    {
        GameClock clock = new();
        for (int i = 0; i < 5; i++) clock.Advance(GameClock.FixedStep);
        int steps = 0;
        while (clock.TryConsumeFixedStep()) steps++;
        TestAssert.True(steps == 5, "game clock consumes one fixed step per advance");
    }

    private static void Advance_ClampsLongFrames()
    {
        GameClock clock = new();
        clock.Advance(0.5);
        TestAssert.True(Math.Abs(clock.DeltaSeconds - 0.25) < 1e-9, "game clock clamps long frames");
    }

    private static void Advance_AccumulatorBounded()
    {
        GameClock clock = new();
        clock.Advance(0.5);
        TestAssert.True(clock.Accumulator <= 0.25, "game clock accumulator is bounded");
    }

    private static void InterpolationAlpha_TracksAccumulator()
    {
        GameClock clock = new();
        clock.Advance(GameClock.FixedStep * 0.5);
        TestAssert.True(Math.Abs(clock.InterpolationAlpha - 0.5) < 1e-9, "interpolation alpha tracks the remaining accumulator");
    }
}
