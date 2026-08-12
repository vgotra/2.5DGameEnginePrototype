namespace Engine.Ecs.Sparse;

public sealed class EntityRegistry
{
    private readonly List<int> _generations = new();
    private readonly List<bool> _alive = new();
    private readonly List<bool> _reserved = new();
    private readonly Stack<int> _free = new();

    public Entity Create()
    {
        if (_free.Count > 0)
        {
            int id = _free.Pop();
            _alive[id] = true;
            return new Entity(id, _generations[id]);
        }

        int next = _generations.Count;
        _generations.Add(1);
        _alive.Add(true);
        _reserved.Add(false);
        return new Entity(next, 1);
    }

    public Entity Reserve()
    {
        if (_free.Count > 0)
        {
            int id = _free.Pop();
            _reserved[id] = true;
            return new Entity(id, _generations[id]);
        }

        int next = _generations.Count;
        _generations.Add(1);
        _alive.Add(false);
        _reserved.Add(true);
        return new Entity(next, 1);
    }

    public bool Activate(Entity entity)
    {
        if (!entity.IsValid || entity.Id >= _generations.Count || !_reserved[entity.Id] || _generations[entity.Id] != entity.Generation)
            return false;
        _reserved[entity.Id] = false;
        _alive[entity.Id] = true;
        return true;
    }

    public void Destroy(Entity entity)
    {
        if (!IsAlive(entity) && !IsReserved(entity)) return;
        _alive[entity.Id] = false;
        _reserved[entity.Id] = false;
        _generations[entity.Id]++;
        _free.Push(entity.Id);
    }

    public bool IsAlive(Entity entity)
        => entity.IsValid && entity.Id < _generations.Count && _alive[entity.Id] && _generations[entity.Id] == entity.Generation;

    public bool IsReserved(Entity entity)
        => entity.IsValid && entity.Id < _generations.Count && _reserved[entity.Id] && _generations[entity.Id] == entity.Generation;
}
