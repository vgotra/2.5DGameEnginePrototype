using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

public struct IntegrateBody : IQueryAction<Position, Velocity, Collider, IntegrateBody>
{
    public TileMap Map;
    public float DeltaSeconds;

    public static void Execute(ref IntegrateBody body, Entity entity, ref Position position, ref Velocity velocity, ref Collider collider)
    {
        position.Value = MovementSystem.Move(body.Map, position.Value, velocity.Value, velocity.Value.Length(), collider.Radius, body.DeltaSeconds);
    }
}
