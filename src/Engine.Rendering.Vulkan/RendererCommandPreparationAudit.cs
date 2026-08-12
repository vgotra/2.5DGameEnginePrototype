using Engine.Threading;

namespace Engine.Rendering.Vulkan;

public readonly record struct RendererAuditResult(int WorkItems, int Chunks, long Checksum);

public static class RendererCommandPreparationAudit
{
    private const int ParallelThreshold = 512;
    private const int MinimumItemsPerChunk = 256;
    private static readonly long[] Partials = new long[64];
    private static int _parallelWorkItems;
    private static int _parallelChunks;
    private static readonly Action<int, int> ParallelBodyAction = ExecuteParallelChunks;

    private readonly record struct ParallelState(int WorkItems, int Chunks, long[] Partials);

    private readonly struct ParallelBody : IParallelForBody<ParallelState, ParallelBody>
    {
        public static void Execute(in ParallelState state, int lo, int hi)
        {
            for (int chunk = lo; chunk < hi; chunk++)
            {
                ChunkBounds(state.WorkItems, state.Chunks, chunk, out int start, out int end);
                long checksum = 0;
                for (int i = start; i < end; i++) checksum += CommandValue(i);
                state.Partials[chunk] = checksum;
            }
        }
    }

    public static int ComputeChunkCount(int workItems, int maxChunks)
    {
        if (workItems <= 0) return 0;
        if (workItems < ParallelThreshold) return 1;
        return Math.Clamp(workItems / MinimumItemsPerChunk, 1, Math.Max(1, maxChunks));
    }

    public static RendererAuditResult PrepareSerial(int workItems, int maxChunks)
    {
        int chunks = ComputeChunkCount(workItems, maxChunks);
        long checksum = 0;
        for (int i = 0; i < workItems; i++) checksum += CommandValue(i);
        return new RendererAuditResult(workItems, chunks, checksum);
    }

    public static RendererAuditResult PrepareParallel(int workItems, int maxChunks, JobSystem jobs)
    {
        int chunks = ComputeChunkCount(workItems, maxChunks);
        if (chunks <= 1) return PrepareSerial(workItems, maxChunks);

        Array.Clear(Partials, 0, chunks);
        _parallelWorkItems = workItems;
        _parallelChunks = chunks;
        jobs.Wait(jobs.ParallelFor(chunks, 1, ParallelBodyAction));

        long total = 0;
        for (int i = 0; i < chunks; i++) total += Partials[i];
        return new RendererAuditResult(workItems, chunks, total);
    }

    private static long CommandValue(int index) => (long)(index + 1) * 6;

    private static void ExecuteParallelChunks(int lo, int hi)
    {
        for (int chunk = lo; chunk < hi; chunk++)
        {
            ChunkBounds(_parallelWorkItems, _parallelChunks, chunk, out int start, out int end);
            long checksum = 0;
            for (int i = start; i < end; i++) checksum += CommandValue(i);
            Partials[chunk] = checksum;
        }
    }

    private static void ChunkBounds(int total, int chunks, int chunk, out int start, out int end)
    {
        int baseSize = total / chunks;
        int remainder = total % chunks;
        start = chunk * baseSize + Math.Min(chunk, remainder);
        end = start + baseSize + (chunk < remainder ? 1 : 0);
    }
}
