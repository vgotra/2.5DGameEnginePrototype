using Engine.Threading;

namespace Engine.Ecs;

internal sealed class QueryParallelDispatch3<T1, T2, T3, TBody>
    where T1 : unmanaged
    where T2 : unmanaged
    where T3 : unmanaged
    where TBody : struct, IForEach<T1, T2, T3, TBody>
{
    private readonly Action<int, int> _runChunks;
    private Query<T1, T2, T3> _query = null!;
    private TBody _body;
    private ComponentArray<T1>[] _arrays1 = [];
    private ComponentArray<T2>[] _arrays2 = [];
    private ComponentArray<T3>[] _arrays3 = [];
    private int[] _chunkArchetype = [];
    private int[] _chunkStart = [];
    private int[] _chunkCount = [];

    public QueryParallelDispatch3() => _runChunks = Run;

    public void Run(JobSystem jobs, ref TBody body, int minChunk, Query<T1, T2, T3> query)
    {
        _query = query;
        _body = body;
        Archetype[] matches = query.Matches;
        int chunkSize = Math.Max(1, minChunk);
        int chunkCount = 0;
        for (int a = 0; a < matches.Length; a++)
            chunkCount += Math.Max(1, (matches[a].Count + chunkSize - 1) / chunkSize);
        EnsureCapacity(chunkCount);
        int w = 0;
        for (int a = 0; a < matches.Length; a++)
        {
            Archetype archetype = matches[a];
            ComponentArray<T1> array1 = archetype.Array<T1>();
            ComponentArray<T2> array2 = archetype.Array<T2>();
            ComponentArray<T3> array3 = archetype.Array<T3>();
            int n = archetype.Count;
            int chunks = Math.Max(1, (n + chunkSize - 1) / chunkSize);
            int baseRows = n / chunks;
            int extra = n % chunks;
            int start = 0;
            for (int c = 0; c < chunks; c++)
            {
                int size = baseRows + (c < extra ? 1 : 0);
                _chunkArchetype[w] = a;
                _chunkStart[w] = start;
                _chunkCount[w] = size;
                _arrays1[w] = array1;
                _arrays2[w] = array2;
                _arrays3[w] = array3;
                start += size;
                w++;
            }
        }
        JobHandle barrier = jobs.ScheduleFor(chunkCount, 1, _runChunks);
        jobs.Complete(barrier);
    }

    private void EnsureCapacity(int count)
    {
        if (_chunkStart.Length >= count) return;
        _chunkArchetype = new int[count];
        _chunkStart = new int[count];
        _chunkCount = new int[count];
        _arrays1 = new ComponentArray<T1>[count];
        _arrays2 = new ComponentArray<T2>[count];
        _arrays3 = new ComponentArray<T3>[count];
    }

    private void Run(int lo, int hi)
    {
        Archetype[] matches = _query.Matches;
        for (int c = lo; c < hi; c++)
        {
            Archetype archetype = matches[_chunkArchetype[c]];
            ComponentArray<T1> array1 = _arrays1[c];
            ComponentArray<T2> array2 = _arrays2[c];
            ComponentArray<T3> array3 = _arrays3[c];
            int start = _chunkStart[c];
            int end = start + _chunkCount[c];
            for (int r = start; r < end; r++)
                TBody.Execute(ref _body, archetype.EntityAt(r), ref array1.Get(r), ref array2.Get(r), ref array3.Get(r));
        }
    }
}
