namespace Engine.Threading;

internal sealed class ParallelForChunk
{
    internal readonly Action Run;
    internal Action<int, int>? Body;
    internal int Lo;
    internal int Hi;
    internal ParallelForChunkPool? Pool;
    internal ParallelForChunk? Next;

    internal ParallelForChunk()
    {
        Run = RunCore;
    }

    private void RunCore()
    {
        Action<int, int>? body = Body;
        if (body != null) body(Lo, Hi);
        Pool?.Return(this);
    }
}
