namespace Engine.Tests;

internal static class SmokeTestRunner
{
    private static readonly (string Name, TestCase[] Tests)[] Suites =
    [
        (nameof(IsometricMathTests), IsometricMathTests.Tests),
        (nameof(EcsTests), EcsTests.Tests),
        (nameof(FrameSchedulerTests), FrameSchedulerTests.Tests),
        (nameof(SparseEcsTests), SparseEcsTests.Tests),
        (nameof(RuntimeContractsTests), RuntimeContractsTests.Tests),
        (nameof(TileMapTests), TileMapTests.Tests),
        (nameof(MovementTests), MovementTests.Tests),
        (nameof(CameraTests), CameraTests.Tests),
        (nameof(RenderExtractionTests), RenderExtractionTests.Tests),
        (nameof(JobSystemTests), JobSystemTests.Tests),
        (nameof(FrameTimerTests), FrameTimerTests.Tests),
        (nameof(GameClockTests), GameClockTests.Tests),
    ];

    public static bool RunAll()
    {
        bool allPassed = true;
        foreach ((string name, TestCase[] tests) in Suites)
        {
            Console.WriteLine($"[{name}]");
            foreach (TestCase test in tests)
            {
                Console.Write($"  {test.Name} ... ");
                try
                {
                    test.Run();
                    Console.WriteLine("passed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED: {ex.Message}");
                    allPassed = false;
                }
            }
        }
        return allPassed;
    }
}
