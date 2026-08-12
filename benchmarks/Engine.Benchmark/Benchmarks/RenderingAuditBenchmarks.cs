using Engine.Rendering.Vulkan;
using Engine.Threading;

namespace Engine.Benchmark.Benchmarks;

internal static class RenderingAuditBenchmarks
{
    private static JobSystem? _jobs;
    private static readonly int[] Workloads = [128, 512, 1_350, 10_000];
    private static int _workloadIndex;

    public static BenchmarkCase[] Create()
    {
        _jobs ??= new JobSystem();
        return
        [
            new BenchmarkCase("RenderingAudit_Serial", 2_000, Serial),
            new BenchmarkCase("RenderingAudit_Parallel", 2_000, Parallel),
        ];
    }

    public static void Dispose() => _jobs?.Dispose();

    private static void Serial()
    {
        int workload = Workloads[_workloadIndex++ & (Workloads.Length - 1)];
        RendererCommandPreparationAudit.PrepareSerial(workload, _jobs!.WorkerCount);
    }

    private static void Parallel()
    {
        int workload = Workloads[_workloadIndex++ & (Workloads.Length - 1)];
        RendererCommandPreparationAudit.PrepareParallel(workload, _jobs!.WorkerCount, _jobs);
    }
}
