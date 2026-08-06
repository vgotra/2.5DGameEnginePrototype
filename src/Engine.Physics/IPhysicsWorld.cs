using System.Numerics;

namespace Engine.Physics;

public interface IPhysicsWorld : IDisposable
{
    PhysicsBodyHandle CreateDynamic(Vector3 position);
    void SetPosition(PhysicsBodyHandle body, Vector3 position);
    Vector3 GetPosition(PhysicsBodyHandle body);
    bool Raycast(PhysicsRay ray, out PhysicsHit hit);
    void Step(float deltaSeconds);
}
