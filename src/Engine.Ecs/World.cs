using Engine.Core;

namespace Engine.Ecs;

public sealed class World
{
    private readonly List<uint> _generations = new();
    private readonly Stack<uint> _free = new();
    private readonly List<Archetype> _archetypes = new();
    private readonly Dictionary<ArchetypeKey, Archetype> _archetypeLookup = new();
    private readonly Dictionary<ArchetypeKey, object> _queryCache = new();
    private readonly List<EntityLocation> _locations = new();
    private readonly Archetype _empty;
    private int _version;

    public World()
    {
        _empty = GetOrCreateArchetype([]);
    }

    public int Version => _version;

    internal List<Archetype> Archetypes => _archetypes;

    public EntityId Create()
    {
        EntityId entity;
        if (_free.Count > 0)
        {
            uint index = _free.Pop();
            entity = new(index, _generations[(int)index]);
        }
        else
        {
            uint index = (uint)_generations.Count;
            _generations.Add(1);
            _locations.Add(default);
            entity = new(index, 1);
        }
        _locations[(int)entity.Index] = new EntityLocation(_empty.Index, _empty.Add(entity));
        _version++;
        return entity;
    }

    public void Destroy(EntityId entity)
    {
        if (!IsAlive(entity)) return;
        _generations[(int)entity.Index]++;
        _free.Push(entity.Index);
        EntityLocation location = _locations[(int)entity.Index];
        _archetypes[location.ArchetypeIndex].RemoveAt(location.Row, _locations);
        _locations[(int)entity.Index] = default;
        _version++;
    }

    public bool IsAlive(EntityId entity)
        => entity.IsValid && entity.Index < _generations.Count && _generations[(int)entity.Index] == entity.Generation;

    public void AddComponent<T>(EntityId entity, in T value) where T : unmanaged
    {
        if (!IsAlive(entity)) return;
        ComponentTypeId id = ComponentTypeId.Of<T>();
        EntityLocation location = _locations[(int)entity.Index];
        Archetype source = _archetypes[location.ArchetypeIndex];
        int existing = source.IndexOf(id);
        if (existing >= 0)
        {
            source.Array<T>().Get(location.Row) = value;
            return;
        }
        (ComponentTypeId[] types, int insertIndex) = InsertSorted(source.Types, id);
        Archetype target = GetOrCreateArchetype(types);
        Move(entity, source, location.Row, target, insertIndex, -1);
        target.Array<T>().Get(target.Count - 1) = value;
    }

    public void RemoveComponent<T>(EntityId entity) where T : unmanaged
    {
        if (!IsAlive(entity)) return;
        ComponentTypeId id = ComponentTypeId.Of<T>();
        EntityLocation location = _locations[(int)entity.Index];
        Archetype source = _archetypes[location.ArchetypeIndex];
        int removeIndex = source.IndexOf(id);
        if (removeIndex < 0) return;
        Archetype target = GetOrCreateArchetype(RemoveSorted(source.Types, removeIndex));
        Move(entity, source, location.Row, target, -1, removeIndex);
    }

    public void SetComponent<T>(EntityId entity, in T value) where T : unmanaged
    {
        if (!IsAlive(entity)) return;
        EntityLocation location = _locations[(int)entity.Index];
        Archetype source = _archetypes[location.ArchetypeIndex];
        source.Array<T>().Get(location.Row) = value;
    }

    public ref T Get<T>(EntityId entity) where T : unmanaged
    {
        EntityLocation location = _locations[(int)entity.Index];
        return ref _archetypes[location.ArchetypeIndex].Array<T>().Get(location.Row);
    }

    public bool HasComponent<T>(EntityId entity) where T : unmanaged
    {
        if (!IsAlive(entity)) return false;
        EntityLocation location = _locations[(int)entity.Index];
        return _archetypes[location.ArchetypeIndex].IndexOf(ComponentTypeId.Of<T>()) >= 0;
    }

    public bool TryGetComponent<T>(EntityId entity, out T value) where T : unmanaged
    {
        if (IsAlive(entity))
        {
            EntityLocation location = _locations[(int)entity.Index];
            Archetype archetype = _archetypes[location.ArchetypeIndex];
            int index = archetype.IndexOf(ComponentTypeId.Of<T>());
            if (index >= 0)
            {
                value = ((ComponentArray<T>)archetype.ArrayAt(index)).Get(location.Row);
                return true;
            }
        }
        value = default;
        return false;
    }

    public Query<T> Query<T>() where T : unmanaged
    {
        ComponentTypeId[] required = QueryTypes<T>.Types;
        ArchetypeKey key = new(required);
        if (_queryCache.TryGetValue(key, out object? cached)) return (Query<T>)cached;
        Query<T> created = new(this, required);
        _queryCache.Add(key, created);
        return created;
    }

    public Query<T1, T2> Query<T1, T2>()
        where T1 : unmanaged
        where T2 : unmanaged
    {
        ComponentTypeId[] required = QueryTypes<T1, T2>.Types;
        ArchetypeKey key = new(required);
        if (_queryCache.TryGetValue(key, out object? cached)) return (Query<T1, T2>)cached;
        Query<T1, T2> created = new(this, required);
        _queryCache.Add(key, created);
        return created;
    }

    public Query<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        ComponentTypeId[] required = QueryTypes<T1, T2, T3>.Types;
        ArchetypeKey key = new(required);
        if (_queryCache.TryGetValue(key, out object? cached)) return (Query<T1, T2, T3>)cached;
        Query<T1, T2, T3> created = new(this, required);
        _queryCache.Add(key, created);
        return created;
    }

    private static class QueryTypes<T> where T : unmanaged
    {
        public static readonly ComponentTypeId[] Types = [ComponentTypeId.Of<T>()];
    }

    private static class QueryTypes<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {
        public static readonly ComponentTypeId[] Types = [ComponentTypeId.Of<T1>(), ComponentTypeId.Of<T2>()];
    }

    private static class QueryTypes<T1, T2, T3>
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        public static readonly ComponentTypeId[] Types = [ComponentTypeId.Of<T1>(), ComponentTypeId.Of<T2>(), ComponentTypeId.Of<T3>()];
    }

    private Archetype GetOrCreateArchetype(ComponentTypeId[] types)
    {
        ArchetypeKey key = new(types);
        if (_archetypeLookup.TryGetValue(key, out Archetype? existing)) return existing;
        Archetype created = new(types) { Index = _archetypes.Count };
        _archetypes.Add(created);
        _archetypeLookup.Add(key, created);
        _version++;
        return created;
    }

    private void Move(EntityId entity, Archetype source, int row, Archetype target, int targetNewIndex, int sourceSkip)
    {
        int targetRow = target.CopyEntityFrom(source, row, targetNewIndex, sourceSkip);
        source.RemoveAt(row, _locations);
        _locations[(int)entity.Index] = new EntityLocation(target.Index, targetRow);
        _version++;
    }

    private static (ComponentTypeId[] Types, int InsertIndex) InsertSorted(ComponentTypeId[] types, ComponentTypeId id)
    {
        ComponentTypeId[] result = new ComponentTypeId[types.Length + 1];
        int insert = 0;
        while (insert < types.Length && types[insert].Value < id.Value) insert++;
        for (int i = 0; i < insert; i++) result[i] = types[i];
        result[insert] = id;
        for (int i = insert; i < types.Length; i++) result[i + 1] = types[i];
        return (result, insert);
    }

    private static ComponentTypeId[] RemoveSorted(ComponentTypeId[] types, int removeIndex)
    {
        ComponentTypeId[] result = new ComponentTypeId[types.Length - 1];
        int w = 0;
        for (int i = 0; i < types.Length; i++) if (i != removeIndex) result[w++] = types[i];
        return result;
    }
}
