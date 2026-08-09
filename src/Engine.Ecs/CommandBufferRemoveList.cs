using Engine.Core;

namespace Engine.Ecs;

internal sealed class CommandBufferRemoveList<T> : ICommandBufferList where T : unmanaged
{
    private readonly List<EntityId> _entities = new();

    public void Add(EntityId entity) => _entities.Add(entity);

    public void Apply(World world)
    {
        for (int i = 0; i < _entities.Count; i++) world.RemoveComponent<T>(_entities[i]);
    }

    public void Clear() => _entities.Clear();
}
