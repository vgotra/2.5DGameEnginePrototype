namespace Engine.Ecs;

public sealed class SystemScheduler
{
    private readonly List<ISystem> _systems = new();
    private readonly List<ISystem> _order = new();
    private long[] _cumulative = [];
    private int _runs;
    private bool _dirty = true;

    public void Register(ISystem system)
    {
        _systems.Add(system);
        _dirty = true;
    }

    public void Run(World world, float deltaSeconds)
    {
        if (_dirty) BuildOrder();
        EnsureCapacity();
        _runs++;
        for (int i = 0; i < _order.Count; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            _order[i].Update(world, deltaSeconds);
            _cumulative[i] += GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }

    public void PrintAndResetCumulative()
    {
        if (_runs == 0) return;
        Console.Write("systems ");
        for (int i = 0; i < _order.Count; i++)
        {
            string name = _order[i].GetType().Name;
            Console.Write($"{name}={(double)_cumulative[i] / _runs:F1} ");
        }
        Console.WriteLine();
        Array.Clear(_cumulative, 0, _cumulative.Length);
        _runs = 0;
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

    private void EnsureCapacity()
    {
        if (_cumulative.Length >= _order.Count) return;
        _cumulative = new long[_order.Count];
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
