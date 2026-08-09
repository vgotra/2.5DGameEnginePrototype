using System.Runtime.CompilerServices;
using Engine.Threading;

namespace Engine.Ecs;

public sealed class Query<T1, T2> where T1 : unmanaged where T2 : unmanaged
{
    private readonly World _world;
    private readonly ComponentTypeId[] _required;
    private Archetype[] _matches = [];
    private int _matchedVersion = -1;

    internal Query(World world, ComponentTypeId[] required)
    {
        _world = world;
        _required = required;
    }

    internal Archetype[] Matches => _matches;

    public int Count
    {
        get
        {
            EnsureMatches();
            int count = 0;
            for (int i = 0; i < _matches.Length; i++) count += _matches[i].Count;
            return count;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ForEach<TBody>(ref TBody body) where TBody : struct, IForEach<T1, T2, TBody>
    {
        EnsureMatches();
        for (int a = 0; a < _matches.Length; a++)
        {
            Archetype archetype = _matches[a];
            ComponentArray<T1> array1 = archetype.Array<T1>();
            ComponentArray<T2> array2 = archetype.Array<T2>();
            int n = archetype.Count;
            for (int r = 0; r < n; r++) TBody.Execute(ref body, archetype.EntityAt(r), ref array1.Get(r), ref array2.Get(r));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void ForEachParallel<TBody>(JobSystem jobs, ref TBody body, int minChunk) where TBody : struct, IForEach<T1, T2, TBody>
    {
        EnsureMatches();
        Dispatcher<TBody>.Instance.Run(jobs, ref body, minChunk, this);
    }

    private void EnsureMatches()
    {
        int version = _world.Version;
        if (_matchedVersion == version) return;
        _matchedVersion = version;
        int count = 0;
        System.Collections.Generic.List<Archetype> all = _world.Archetypes;
        for (int i = 0; i < all.Count; i++) if (ContainsAll(all[i])) count++;
        if (_matches.Length != count) _matches = new Archetype[count];
        int w = 0;
        for (int i = 0; i < all.Count; i++) if (ContainsAll(all[i])) _matches[w++] = all[i];
    }

    private bool ContainsAll(Archetype archetype)
    {
        for (int i = 0; i < _required.Length; i++)
            if (archetype.IndexOf(_required[i]) < 0) return false;
        return true;
    }

    private static class Dispatcher<TBody> where TBody : struct, IForEach<T1, T2, TBody>
    {
        public static readonly QueryParallelDispatch2<T1, T2, TBody> Instance = new();
    }
}
