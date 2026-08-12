namespace Engine.Ecs.Sparse;

public interface IQueryAction<T, TAction>
    where T : unmanaged
    where TAction : struct, IQueryAction<T, TAction>
{
    static abstract void Execute(ref TAction action, Entity entity, ref T component);
}

public interface IQueryAction<T1, T2, TAction>
    where T1 : unmanaged
    where T2 : unmanaged
    where TAction : struct, IQueryAction<T1, T2, TAction>
{
    static abstract void Execute(ref TAction action, Entity entity, ref T1 first, ref T2 second);
}

public interface IQueryAction<T1, T2, T3, TAction>
    where T1 : unmanaged
    where T2 : unmanaged
    where T3 : unmanaged
    where TAction : struct, IQueryAction<T1, T2, T3, TAction>
{
    static abstract void Execute(ref TAction action, Entity entity, ref T1 first, ref T2 second, ref T3 third);
}
