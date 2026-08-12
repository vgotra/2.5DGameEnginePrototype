namespace Engine.Ecs.Sparse;

public sealed class EntityRegistry
{
    private readonly List<int> _generations = new();
    private readonly List<bool> _alive = new();
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
        return new Entity(next, 1);
    }

    public void Destroy(Entity entity)
    {
        if (!IsAlive(entity)) return;
        _alive[entity.Id] = false;
        _generations[entity.Id]++;
        _free.Push(entity.Id);
    }

    public bool IsAlive(Entity entity)
        => entity.IsValid && entity.Id < _generations.Count && _alive[entity.Id] && _generations[entity.Id] == entity.Generation;
}
