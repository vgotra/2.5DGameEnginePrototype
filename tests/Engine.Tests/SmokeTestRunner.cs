namespace Engine.Tests;

internal static class SmokeTestRunner
{
    private static readonly (string Name, TestCase[] Tests)[] Suites =
    [
        (nameof(IsometricMathTests), IsometricMathTests.Tests),
        (nameof(SparseFrameSchedulerTests), SparseFrameSchedulerTests.Tests),
        (nameof(SparseEcsTests), SparseEcsTests.Tests),
        (nameof(RuntimeContractsTests), RuntimeContractsTests.Tests),
        (nameof(PublicGameplayApiTests), PublicGameplayApiTests.Tests),
        (nameof(GameApplicationTests), GameApplicationTests.Tests),
        (nameof(GameplayFoundationTests), GameplayFoundationTests.Tests),
        (nameof(GameplayRuntimeTests), GameplayRuntimeTests.Tests),
        (nameof(GameplayRuntimeScenarioTests), GameplayRuntimeScenarioTests.Tests),
        (nameof(GltfLoaderTests), GltfLoaderTests.Tests),
        (nameof(TerrainCollisionTests), TerrainCollisionTests.Tests),
        (nameof(TerrainSurfaceTests), TerrainSurfaceTests.Tests),
        (nameof(MovementTests), MovementTests.Tests),
        (nameof(JumpTests), JumpTests.Tests),
        (nameof(CameraTests), CameraTests.Tests),
        (nameof(RenderExtractionTests), RenderExtractionTests.Tests),
        (nameof(FeaturePipelineTests), FeaturePipelineTests.Tests),
        (nameof(JobSystemTests), JobSystemTests.Tests),
        (nameof(FrameTimerTests), FrameTimerTests.Tests),
        (nameof(FrameMetricsTests), FrameMetricsTests.Tests),
        (nameof(GameClockTests), GameClockTests.Tests),
        (nameof(PresentationHistoryTests), PresentationHistoryTests.Tests),
        (nameof(ArpgWorkloadTests), ArpgWorkloadTests.Tests),
        (nameof(ArpgCombatTests), ArpgCombatTests.Tests),
        (nameof(RenderingAuditTests), [new TestCase("Renderer command preparation", RenderingAuditTests.Run)]),
        (nameof(TextureAssetTests), TextureAssetTests.Tests),
        (nameof(TextureUploadPolicyTests), TextureUploadPolicyTests.Tests),
        (nameof(DescriptorModeTests), DescriptorModeTests.Tests),
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
