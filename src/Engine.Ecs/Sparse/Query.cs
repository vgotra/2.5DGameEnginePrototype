using Engine.Threading;

namespace Engine.Ecs.Sparse;

public sealed class Query<T> where T : unmanaged
{
    private const int ParallelThreshold = 512;
    private readonly ComponentStore<T> _store;
    internal Query(ComponentStore<T> store) => _store = store;
    public int Count => _store.Count;

    public void ForEach<TAction>(ref TAction action) where TAction : struct, IQueryAction<T, TAction>
    {
        ReadOnlySpan<Entity> entities = _store.DenseEntities;
        for (int i = 0; i < entities.Length; i++)
            TAction.Execute(ref action, entities[i], ref _store.DenseComponent(i));
    }

    public void ParallelForEach<TAction>(JobSystem jobs, int minChunkSize = 64)
        where TAction : struct, IParallelQueryAction<T, TAction>
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ReadOnlySpan<Entity> entities = _store.DenseEntities;
        if (entities.Length < ParallelThreshold)
        {
            for (int i = 0; i < entities.Length; i++) TAction.Execute(entities[i], ref _store.DenseComponent(i));
            return;
        }
        ParallelQueryContext state = new(_store);
        JobHandle handle = jobs.ParallelFor<ParallelQueryContext, ParallelQueryBody<TAction>>(entities.Length, Math.Max(1, minChunkSize), state);
        jobs.Wait(handle);
    }

    private readonly struct ParallelQueryContext
    {
        internal readonly ComponentStore<T> Store;
        internal ParallelQueryContext(ComponentStore<T> store) => Store = store;
    }

    private readonly struct ParallelQueryBody<TAction> : IParallelForBody<ParallelQueryContext, ParallelQueryBody<TAction>>
        where TAction : struct, IParallelQueryAction<T, TAction>
    {
        public static void Execute(in ParallelQueryContext state, int lo, int hi)
        {
            for (int i = lo; i < hi; i++) TAction.Execute(state.Store.DenseEntities[i], ref state.Store.DenseComponent(i));
        }
    }
}

public sealed class Query<T1, T2>
    where T1 : unmanaged
    where T2 : unmanaged
{
    private readonly ComponentStore<T1> _first;
    private readonly ComponentStore<T2> _second;

    internal Query(ComponentStore<T1> first, ComponentStore<T2> second)
    {
        _first = first;
        _second = second;
    }

    public int Count
    {
        get
        {
            ComponentStore<T1> first = _first;
            ComponentStore<T2> second = _second;
            int count = 0;
            bool firstDrives = first.Count <= second.Count;
            ReadOnlySpan<Entity> entities = firstDrives ? first.DenseEntities : second.DenseEntities;
            if (firstDrives)
            {
                for (int i = 0; i < entities.Length; i++) if (second.Has(entities[i])) count++;
            }
            else
            {
                for (int i = 0; i < entities.Length; i++) if (first.Has(entities[i])) count++;
            }
            return count;
        }
    }

    public void ForEach<TAction>(ref TAction action) where TAction : struct, IQueryAction<T1, T2, TAction>
    {
        ReadOnlySpan<Entity> entities = _first.Count <= _second.Count ? _first.DenseEntities : _second.DenseEntities;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!_first.TryGetDenseIndex(entity, out int firstIndex) || !_second.TryGetDenseIndex(entity, out int secondIndex)) continue;
            TAction.Execute(ref action, entity, ref _first.DenseComponent(firstIndex), ref _second.DenseComponent(secondIndex));
        }
    }

    public void ParallelForEach<TAction>(JobSystem jobs, int minChunkSize = 64)
        where TAction : struct, IParallelQueryAction<T1, T2, TAction>
    {
        ArgumentNullException.ThrowIfNull(jobs);
        bool firstDrives = _first.Count <= _second.Count;
        int driverCount = firstDrives ? _first.Count : _second.Count;
        if (driverCount < 512)
        {
            RunSerial<TAction>();
            return;
        }
        ParallelQueryContext state = new(_first, _second, firstDrives);
        JobHandle handle = jobs.ParallelFor<ParallelQueryContext, ParallelQueryBody<TAction>>(driverCount, Math.Max(1, minChunkSize), state);
        jobs.Wait(handle);
    }

    private void RunSerial<TAction>() where TAction : struct, IParallelQueryAction<T1, T2, TAction>
    {
        ReadOnlySpan<Entity> entities = _first.Count <= _second.Count ? _first.DenseEntities : _second.DenseEntities;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!_first.TryGetDenseIndex(entity, out int firstIndex) || !_second.TryGetDenseIndex(entity, out int secondIndex)) continue;
            TAction.Execute(entity, ref _first.DenseComponent(firstIndex), ref _second.DenseComponent(secondIndex));
        }
    }

    private readonly struct ParallelQueryContext
    {
        internal readonly ComponentStore<T1> First;
        internal readonly ComponentStore<T2> Second;
        internal readonly bool FirstDrives;
        internal ParallelQueryContext(ComponentStore<T1> first, ComponentStore<T2> second, bool firstDrives) { First = first; Second = second; FirstDrives = firstDrives; }
    }

    private readonly struct ParallelQueryBody<TAction> : IParallelForBody<ParallelQueryContext, ParallelQueryBody<TAction>>
        where TAction : struct, IParallelQueryAction<T1, T2, TAction>
    {
        public static void Execute(in ParallelQueryContext state, int lo, int hi)
        {
            ReadOnlySpan<Entity> entities = state.FirstDrives ? state.First.DenseEntities : state.Second.DenseEntities;
            for (int i = lo; i < hi; i++)
            {
                Entity entity = entities[i];
                if (!state.First.TryGetDenseIndex(entity, out int firstIndex) || !state.Second.TryGetDenseIndex(entity, out int secondIndex)) continue;
                TAction.Execute(entity, ref state.First.DenseComponent(firstIndex), ref state.Second.DenseComponent(secondIndex));
            }
        }
    }
}

public sealed class Query<T1, T2, T3>
    where T1 : unmanaged
    where T2 : unmanaged
    where T3 : unmanaged
{
    private readonly ComponentStore<T1> _first;
    private readonly ComponentStore<T2> _second;
    private readonly ComponentStore<T3> _third;

    internal Query(ComponentStore<T1> first, ComponentStore<T2> second, ComponentStore<T3> third)
    {
        _first = first;
        _second = second;
        _third = third;
    }

    public int Count
    {
        get
        {
            int count = 0;
            ReadOnlySpan<Entity> entities = DriverEntities();
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (_first.Has(entity) && _second.Has(entity) && _third.Has(entity)) count++;
            }
            return count;
        }
    }

    public void ForEach<TAction>(ref TAction action) where TAction : struct, IQueryAction<T1, T2, T3, TAction>
    {
        ReadOnlySpan<Entity> entities = DriverEntities();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!_first.TryGetDenseIndex(entity, out int firstIndex) || !_second.TryGetDenseIndex(entity, out int secondIndex) || !_third.TryGetDenseIndex(entity, out int thirdIndex)) continue;
            TAction.Execute(ref action, entity, ref _first.DenseComponent(firstIndex), ref _second.DenseComponent(secondIndex), ref _third.DenseComponent(thirdIndex));
        }
    }

    public void ParallelForEach<TAction>(JobSystem jobs, int minChunkSize = 64)
        where TAction : struct, IParallelQueryAction<T1, T2, T3, TAction>
    {
        ArgumentNullException.ThrowIfNull(jobs);
        int driver = DriverIndex();
        int driverCount = driver == 0 ? _first.Count : driver == 1 ? _second.Count : _third.Count;
        if (driverCount < 512)
        {
            RunSerial<TAction>();
            return;
        }
        ParallelQueryContext state = new(_first, _second, _third, driver);
        JobHandle handle = jobs.ParallelFor<ParallelQueryContext, ParallelQueryBody<TAction>>(driverCount, Math.Max(1, minChunkSize), state);
        jobs.Wait(handle);
    }

    private int DriverIndex()
        => _first.Count <= _second.Count && _first.Count <= _third.Count ? 0 : _second.Count <= _third.Count ? 1 : 2;

    private void RunSerial<TAction>() where TAction : struct, IParallelQueryAction<T1, T2, T3, TAction>
    {
        ReadOnlySpan<Entity> entities = DriverEntities();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!_first.TryGetDenseIndex(entity, out int firstIndex) || !_second.TryGetDenseIndex(entity, out int secondIndex) || !_third.TryGetDenseIndex(entity, out int thirdIndex)) continue;
            TAction.Execute(entity, ref _first.DenseComponent(firstIndex), ref _second.DenseComponent(secondIndex), ref _third.DenseComponent(thirdIndex));
        }
    }

    private readonly struct ParallelQueryContext
    {
        internal readonly ComponentStore<T1> First; internal readonly ComponentStore<T2> Second; internal readonly ComponentStore<T3> Third; internal readonly int Driver;
        internal ParallelQueryContext(ComponentStore<T1> first, ComponentStore<T2> second, ComponentStore<T3> third, int driver) { First = first; Second = second; Third = third; Driver = driver; }
    }

    private readonly struct ParallelQueryBody<TAction> : IParallelForBody<ParallelQueryContext, ParallelQueryBody<TAction>>
        where TAction : struct, IParallelQueryAction<T1, T2, T3, TAction>
    {
        public static void Execute(in ParallelQueryContext state, int lo, int hi)
        {
            ReadOnlySpan<Entity> entities = state.Driver == 0 ? state.First.DenseEntities : state.Driver == 1 ? state.Second.DenseEntities : state.Third.DenseEntities;
            for (int i = lo; i < hi; i++)
            {
                Entity entity = entities[i];
                if (!state.First.TryGetDenseIndex(entity, out int firstIndex) || !state.Second.TryGetDenseIndex(entity, out int secondIndex) || !state.Third.TryGetDenseIndex(entity, out int thirdIndex)) continue;
                TAction.Execute(entity, ref state.First.DenseComponent(firstIndex), ref state.Second.DenseComponent(secondIndex), ref state.Third.DenseComponent(thirdIndex));
            }
        }
    }

    private ReadOnlySpan<Entity> DriverEntities()
    {
        if (_first.Count <= _second.Count && _first.Count <= _third.Count) return _first.DenseEntities;
        return _second.Count <= _third.Count ? _second.DenseEntities : _third.DenseEntities;
    }
}
