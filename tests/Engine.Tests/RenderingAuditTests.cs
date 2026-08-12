using Engine.Rendering.Vulkan;
using Engine.Threading;

namespace Engine.Tests;

internal static class RenderingAuditTests
{
    public static void Run()
    {
        Assert(RendererCommandPreparationAudit.ComputeChunkCount(0, 8) == 0, "empty workload chunks");
        Assert(RendererCommandPreparationAudit.ComputeChunkCount(128, 8) == 1, "small workload chunks");
        Assert(RendererCommandPreparationAudit.ComputeChunkCount(10_000, 4) == 4, "large workload chunks");

        using JobSystem jobs = new(4);
        int[] workloads = [512, 1_350, 10_000];
        for (int i = 0; i < workloads.Length; i++)
        {
            RendererAuditResult serial = RendererCommandPreparationAudit.PrepareSerial(workloads[i], 4);
            RendererAuditResult parallel = RendererCommandPreparationAudit.PrepareParallel(workloads[i], 4, jobs);
            Assert(serial == parallel, $"serial/parallel parity {workloads[i]}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
