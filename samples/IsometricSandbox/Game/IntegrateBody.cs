using Engine.App;
using Engine.Core;
using Engine.Ecs;

namespace IsometricSandbox.Game;

public struct IntegrateBody : IForEach<Position, Velocity, Collider, IntegrateBody>
{
    public TileMap Map;
    public float DeltaSeconds;

    public static void Execute(ref IntegrateBody body, EntityId entity, ref Position position, ref Velocity velocity, ref Collider collider)
    {
        position.Value = MovementSystem.Move(body.Map, position.Value, velocity.Value, 1f, collider.Radius, body.DeltaSeconds);
    }
}
