using Engine.Core;

namespace Engine.Ecs;

public interface IForEach<T1, TBody> where T1 : unmanaged where TBody : struct, IForEach<T1, TBody>
{
    static abstract void Execute(ref TBody body, EntityId entity, ref T1 a);
}
