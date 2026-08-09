namespace Engine.Ecs;

public readonly record struct ComponentTypeId(int Value)
{
    public static ComponentTypeId Of<T>() where T : unmanaged => ComponentRegistry<T>.Id;

    private static class ComponentRegistry<T> where T : unmanaged
    {
        public static readonly ComponentTypeId Id = ComponentTypeRegistry.Next<T>();
    }
}
