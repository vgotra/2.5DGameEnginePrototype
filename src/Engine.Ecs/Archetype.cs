using Engine.Core;

namespace Engine.Ecs;

internal sealed class Archetype
{
    private readonly IComponentArray[] _arrays;
    private EntityId[] _entities = [];

    public ComponentTypeId[] Types { get; }
    public int Index { get; set; }
    public int Count { get; private set; }

    public Archetype(ComponentTypeId[] types)
    {
        Types = types;
        _arrays = new IComponentArray[types.Length];
        for (int i = 0; i < types.Length; i++) _arrays[i] = ComponentTypeRegistry.CreateArray(types[i]);
    }

    public EntityId EntityAt(int row) => _entities[row];

    public int IndexOf(ComponentTypeId id)
    {
        for (int i = 0; i < Types.Length; i++) if (Types[i] == id) return i;
        return -1;
    }

    public ComponentArray<T> Array<T>() where T : unmanaged
    {
        int index = IndexOf(ComponentTypeId.Of<T>());
        if (index < 0) throw new InvalidOperationException($"Archetype does not contain component '{typeof(T).Name}'.");
        return (ComponentArray<T>)_arrays[index];
    }

    public IComponentArray ArrayAt(int index) => _arrays[index];

    public int Add(EntityId entity)
    {
        int row = Count;
        EnsureCapacity(Count + 1);
        _entities[row] = entity;
        Count++;
        return row;
    }

    public int CopyEntityFrom(Archetype source, int row, int targetNewIndex, int sourceSkip)
    {
        int targetRow = Count;
        EnsureCapacity(Count + 1);
        int src = 0;
        for (int t = 0; t < Types.Length; t++)
        {
            if (t == targetNewIndex) continue;
            if (src == sourceSkip) src++;
            _arrays[t].CopyRowFrom(source._arrays[src], row, targetRow);
            src++;
        }
        _entities[targetRow] = source._entities[row];
        Count++;
        return targetRow;
    }

    public void RemoveAt(int row, List<EntityLocation> locations)
    {
        int last = Count - 1;
        EntityId moved = _entities[last];
        if (row != last)
        {
            for (int i = 0; i < _arrays.Length; i++) _arrays[i].SwapRemove(row, last);
            _entities[row] = moved;
            locations[(int)moved.Index] = new EntityLocation(Index, row);
        }
        Count--;
    }

    private void EnsureCapacity(int count)
    {
        if (_entities.Length < count)
        {
            int newSize = _entities.Length == 0 ? 16 : Math.Max(count, _entities.Length * 2);
            System.Array.Resize(ref _entities, newSize);
        }
        for (int i = 0; i < _arrays.Length; i++) _arrays[i].EnsureCapacity(count);
    }
}
