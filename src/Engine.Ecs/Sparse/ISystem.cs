namespace Engine.Ecs.Sparse;

public interface ISystem
{
    void Update(World world, float deltaSeconds);
}
