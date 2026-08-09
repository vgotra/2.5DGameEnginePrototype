namespace Engine.Ecs;

public readonly struct ComponentAccess
{
    public readonly ComponentTypeId[] ReadTypes;
    public readonly ComponentTypeId[] WriteTypes;

    private ComponentAccess(ComponentTypeId[] readTypes, ComponentTypeId[] writeTypes)
    {
        ReadTypes = readTypes;
        WriteTypes = writeTypes;
    }

    public static ComponentAccess Read<T>() where T : unmanaged => new([ComponentTypeId.Of<T>()], []);
    public static ComponentAccess Write<T>() where T : unmanaged => new([], [ComponentTypeId.Of<T>()]);
    public static ComponentAccess Read<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        => new([ComponentTypeId.Of<T1>(), ComponentTypeId.Of<T2>()], []);
    public static ComponentAccess Write<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        => new([], [ComponentTypeId.Of<T1>(), ComponentTypeId.Of<T2>()]);
    public static ComponentAccess ReadWrite<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        => new([ComponentTypeId.Of<T1>()], [ComponentTypeId.Of<T2>()]);
    public static ComponentAccess ReadWrite<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        => new([ComponentTypeId.Of<T1>()], [ComponentTypeId.Of<T2>(), ComponentTypeId.Of<T3>()]);
    public static ComponentAccess ReadWrite<T1, T2, T3, T4>()
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        => new([ComponentTypeId.Of<T1>(), ComponentTypeId.Of<T2>()], [ComponentTypeId.Of<T3>(), ComponentTypeId.Of<T4>()]);
}
