namespace Engine.Ecs;

public sealed class SystemScheduler
{
    private readonly List<ISystem> _systems = new();
    private readonly List<ISystem> _order = new();
    private bool _dirty = true;

    public void Register(ISystem system)
    {
        _systems.Add(system);
        _dirty = true;
    }

    public void Run(World world, float deltaSeconds)
    {
        if (_dirty) BuildOrder();
        for (int i = 0; i < _order.Count; i++)
            _order[i].Update(world, deltaSeconds);
    }

    private void BuildOrder()
    {
        _dirty = false;
        _order.Clear();
        for (int i = 0; i < _systems.Count; i++)
        {
            ISystem system = _systems[i];
            ComponentAccess access = system.Access;
            int insertAt = _order.Count;
            for (int j = _order.Count - 1; j >= 0; j--)
            {
                if (Conflicts(access, _order[j].Access))
                {
                    insertAt = j + 1;
                    break;
                }
            }
            _order.Insert(insertAt, system);
        }
    }

    private static bool Conflicts(ComponentAccess a, ComponentAccess b)
        => Overlaps(a.WriteTypes, b.ReadTypes) || Overlaps(a.WriteTypes, b.WriteTypes) || Overlaps(a.ReadTypes, b.WriteTypes);

    private static bool Overlaps(ComponentTypeId[] left, ComponentTypeId[] right)
    {
        for (int i = 0; i < left.Length; i++)
            for (int j = 0; j < right.Length; j++)
                if (left[i] == right[j]) return true;
        return false;
    }
}
