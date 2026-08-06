using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsRay(Vector3 Origin, Vector3 Direction, float Distance);
