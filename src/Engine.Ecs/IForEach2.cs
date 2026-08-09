using Engine.Core;

namespace Engine.Ecs;

public interface IForEach<T1, T2, TBody>
    where T1 : unmanaged
    where T2 : unmanaged
    where TBody : struct, IForEach<T1, T2, TBody>
{
    static abstract void Execute(ref TBody body, EntityId entity, ref T1 a, ref T2 b);
}
