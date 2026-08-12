namespace Engine.Ecs.Sparse;

public sealed class Query<T> where T : unmanaged
{
    private readonly ComponentStore<T> _store;
    internal Query(ComponentStore<T> store) => _store = store;
    public int Count => _store.Count;

    public void ForEach<TAction>(ref TAction action) where TAction : struct, IQueryAction<T, TAction>
    {
        ReadOnlySpan<Entity> entities = _store.DenseEntities;
        for (int i = 0; i < entities.Length; i++)
            TAction.Execute(ref action, entities[i], ref _store.DenseComponent(i));
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

    private ReadOnlySpan<Entity> DriverEntities()
    {
        if (_first.Count <= _second.Count && _first.Count <= _third.Count) return _first.DenseEntities;
        return _second.Count <= _third.Count ? _second.DenseEntities : _third.DenseEntities;
    }
}
