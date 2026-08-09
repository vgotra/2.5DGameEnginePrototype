using Engine.Core;

namespace Engine.Ecs;

public sealed class WorldCommandBuffer
{
    private readonly List<EntityId> _destroys = new();
    private readonly Dictionary<ComponentTypeId, ICommandBufferList> _adds = new();
    private readonly List<ComponentTypeId> _addOrder = new();
    private readonly Dictionary<ComponentTypeId, ICommandBufferList> _removes = new();
    private readonly List<ComponentTypeId> _removeOrder = new();

    public void Destroy(EntityId entity) => _destroys.Add(entity);

    public void AddComponent<T>(EntityId entity, in T value) where T : unmanaged
    {
        ComponentTypeId id = ComponentTypeId.Of<T>();
        if (_adds.TryGetValue(id, out ICommandBufferList? existing))
        {
            ((CommandBufferList<T>)existing).Add(entity, in value);
            return;
        }
        CommandBufferList<T> list = new();
        _adds.Add(id, list);
        _addOrder.Add(id);
        list.Add(entity, in value);
    }

    public void RemoveComponent<T>(EntityId entity) where T : unmanaged
    {
        ComponentTypeId id = ComponentTypeId.Of<T>();
        if (_removes.TryGetValue(id, out ICommandBufferList? existing))
        {
            ((CommandBufferRemoveList<T>)existing).Add(entity);
            return;
        }
        CommandBufferRemoveList<T> list = new();
        _removes.Add(id, list);
        _removeOrder.Add(id);
        list.Add(entity);
    }

    public void Clear()
    {
        _destroys.Clear();
        for (int i = 0; i < _addOrder.Count; i++) _adds[_addOrder[i]].Clear();
        _addOrder.Clear();
        for (int i = 0; i < _removeOrder.Count; i++) _removes[_removeOrder[i]].Clear();
        _removeOrder.Clear();
    }

    public void Apply(World world)
    {
        for (int i = 0; i < _addOrder.Count; i++) _adds[_addOrder[i]].Apply(world);
        for (int i = 0; i < _removeOrder.Count; i++) _removes[_removeOrder[i]].Apply(world);
        for (int i = 0; i < _destroys.Count; i++) world.Destroy(_destroys[i]);
    }
}
