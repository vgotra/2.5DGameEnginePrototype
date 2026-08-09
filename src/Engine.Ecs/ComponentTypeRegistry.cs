namespace Engine.Ecs;

internal static class ComponentTypeRegistry
{
    private static Func<IComponentArray>[] _factories = new Func<IComponentArray>[64];
    private static int _counter;
    private static readonly object Sync = new();

    public static ComponentTypeId Next<T>() where T : unmanaged
    {
        int id = Interlocked.Increment(ref _counter) - 1;
        EnsureCapacity(id);
        Volatile.Write(ref _factories[id], static () => new ComponentArray<T>());
        return new ComponentTypeId(id);
    }

    public static IComponentArray CreateArray(ComponentTypeId id)
        => _factories[id.Value]();

    private static void EnsureCapacity(int id)
    {
        if (id < _factories.Length) return;
        lock (Sync)
        {
            if (id >= _factories.Length)
            {
                int size = Math.Max(id + 1, _factories.Length * 2);
                Array.Resize(ref _factories, size);
            }
        }
    }
}
