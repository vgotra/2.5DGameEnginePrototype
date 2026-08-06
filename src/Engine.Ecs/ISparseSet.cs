using Engine.Core;

namespace Engine.Ecs;

public interface ISparseSet
{
    int Count { get; }
    bool Remove(EntityId entity);
}
