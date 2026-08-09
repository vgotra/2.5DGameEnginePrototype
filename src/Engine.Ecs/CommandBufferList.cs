using Engine.Core;

namespace Engine.Ecs;

internal sealed class CommandBufferList<T> : ICommandBufferList where T : unmanaged
{
    private readonly List<(EntityId Entity, T Value)> _items = new();

    public void Add(EntityId entity, in T value) => _items.Add((entity, value));

    public void Apply(World world)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            (EntityId entity, T value) = _items[i];
            world.AddComponent(entity, in value);
        }
    }

    public void Clear() => _items.Clear();
}
