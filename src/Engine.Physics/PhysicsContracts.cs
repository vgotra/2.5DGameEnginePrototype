using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsBodyHandle(int Value);
public readonly record struct PhysicsRay(Vector3 Origin, Vector3 Direction, float Distance);
public readonly record struct PhysicsHit(PhysicsBodyHandle Body, Vector3 Position, Vector3 Normal, float Distance);

public interface IPhysicsWorld : IDisposable
{
    PhysicsBodyHandle CreateDynamic(Vector3 position);
    void SetPosition(PhysicsBodyHandle body, Vector3 position);
    Vector3 GetPosition(PhysicsBodyHandle body);
    bool Raycast(PhysicsRay ray, out PhysicsHit hit);
    void Step(float deltaSeconds);
}
