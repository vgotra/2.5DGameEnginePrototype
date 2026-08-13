using System.Numerics;
using Engine.App;
using Engine.Rendering;
using IsometricSandbox.Game;

namespace Engine.Tests;

internal static class ArpgWorkloadTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Population_IsFixed), Population_IsFixed),
        new(nameof(SeededWorkloads_AreDeterministic), SeededWorkloads_AreDeterministic),
        new(nameof(ExecutionModes_StayParityEquivalent), ExecutionModes_StayParityEquivalent),
        new(nameof(AdaptivePolicy_UsesThreshold), AdaptivePolicy_UsesThreshold),
        new(nameof(Extraction_ProjectsGameplaySizedSprites), Extraction_ProjectsGameplaySizedSprites),
        new(nameof(Options_ParseArpgWithoutChangingSimulation), Options_ParseArpgWithoutChangingSimulation),
        new(nameof(Options_DefaultAndCustomFrameCaps), Options_DefaultAndCustomFrameCaps),
    ];

    private static TerrainSurface Grid() => new(20, 20, 1f, 64f, 32f, 7);

    private static void Population_IsFixed()
    {
        ArpgWorkload workload = new();
        ArpgWorkloadSnapshot snapshot = workload.Tick(ArpgExecutionMode.Serial);
        TestAssert.True(snapshot.Players == 1 && snapshot.Monsters == 250 && snapshot.Projectiles == 100 && snapshot.Effects == 500, "ARPG population is fixed");
    }

    private static void SeededWorkloads_AreDeterministic()
    {
        ArpgWorkload left = new(7);
        ArpgWorkload right = new(7);
        for (int i = 0; i < 10; i++)
            TestAssert.True(left.Tick(ArpgExecutionMode.Serial).Checksum == right.Tick(ArpgExecutionMode.Serial).Checksum, "same seed stays deterministic");
    }

    private static void ExecutionModes_StayParityEquivalent()
    {
        ArpgWorkload serial = new(9);
        ArpgWorkload adaptive = new(9);
        ArpgWorkload forced = new(9);
        for (int i = 0; i < 10; i++)
        {
            int checksum = serial.Tick(ArpgExecutionMode.Serial).Checksum;
            TestAssert.True(checksum == adaptive.Tick(ArpgExecutionMode.AdaptiveParallel).Checksum && checksum == forced.Tick(ArpgExecutionMode.ForcedParallel).Checksum, "execution modes preserve state parity");
        }
    }

    private static void AdaptivePolicy_UsesThreshold()
    {
        ArpgWorkload workload = new();
        workload.Tick(ArpgExecutionMode.AdaptiveParallel);
        TestAssert.True(workload.LastParallelDecision, "adaptive mode selects parallel for the ARPG population");
        workload.Tick(ArpgExecutionMode.Serial);
        TestAssert.True(!workload.LastParallelDecision, "serial mode stays serial");
        workload.Tick(ArpgExecutionMode.ForcedParallel);
        TestAssert.True(workload.LastParallelDecision, "forced mode selects parallel");
    }

    private static void Extraction_ProjectsGameplaySizedSprites()
    {
        TerrainSurface grid = Grid();
        IsometricCamera camera = new(new Vector2(800, 600));
        camera.Follow(new Vector2(10, 10), grid);
        ArpgWorkload workload = new();
        workload.Tick(ArpgExecutionMode.Serial);
        SpritePacket[] packets = new SpritePacket[851];
        int count = workload.Extract(camera, grid, packets);
        TestAssert.True(count == 851, "all ARPG packets are extracted");
        TestAssert.True(packets[0].Size == new Vector2(44, 56) && packets[1].Size.X >= 28, "ARPG sprites use gameplay dimensions");
        TestAssert.True(packets[0].Position.X > 0 && packets[0].Position.X < 800 && packets[0].Position.Y > 0 && packets[0].Position.Y < 600, "ARPG positions are projected into the viewport");
    }

    private static void Options_ParseArpgWithoutChangingSimulation()
    {
        Options arpg = Options.Parse(["--arpg"]);
        Options simulation = Options.Parse(["--simulation"]);
        TestAssert.True(arpg.Arpg && !arpg.Simulation && simulation.Simulation && !simulation.Arpg, "sample options preserve ARPG and simulation modes");
    }

    private static void Options_DefaultAndCustomFrameCaps()
    {
        TestAssert.True(Options.Parse([]).FrameCap == 120, "sample defaults to 120 FPS rendering");
        TestAssert.True(Options.Parse(["--cap", "240"]).FrameCap == 240, "sample accepts a custom render cap");
        TestAssert.True(Options.Parse(["--cap", "0"]).FrameCap == 0, "zero selects uncapped rendering");
    }
}
