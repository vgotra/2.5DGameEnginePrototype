using System.Numerics;

namespace Engine.Physics;

public readonly record struct PhysicsHit(PhysicsBodyHandle Body, Vector3 Position, Vector3 Normal, float Distance);
