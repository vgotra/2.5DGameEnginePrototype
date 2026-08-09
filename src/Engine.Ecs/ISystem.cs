namespace Engine.Ecs;

public interface ISystem
{
    ComponentAccess Access { get; }
    void Update(World world, float deltaSeconds);
}
