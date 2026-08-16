using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game.Gameplay.Systems;

public struct IntegrateBody : IQueryAction<Position, Velocity, Collider, IntegrateBody>
{
    public TerrainSurface Map;
    public float DeltaSeconds;

    public static void Execute(ref IntegrateBody body, Entity entity, ref Position position, ref Velocity velocity, ref Collider collider)
    {
        position.Value = MovementSystem.MoveVelocity(body.Map, position.Value, velocity.Value, collider.Radius, body.DeltaSeconds);
    }
}
