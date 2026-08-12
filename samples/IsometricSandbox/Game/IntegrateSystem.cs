using Engine.App;
using Engine.Core;
using Engine.Ecs;
using World = Engine.Ecs.World;

namespace IsometricSandbox.Game;

// Applies a velocity to a position with map collision. Only entities that
// carry a Collider participate; the archer is the sole user today.
public sealed class IntegrateSystem(TileMap map) : ISystem
{
    public EntityId Player { get; set; }

    public ComponentAccess Access => ComponentAccess.ReadWrite<Velocity, Position>();

    public void Update(World world, float deltaSeconds)
    {
        ref Position position = ref world.Get<Position>(Player);
        ref Velocity velocity = ref world.Get<Velocity>(Player);
        ref Collider collider = ref world.Get<Collider>(Player);
        IntegrateBody body = new() { Map = map, DeltaSeconds = deltaSeconds };
        IntegrateBody.Execute(ref body, Player, ref position, ref velocity, ref collider);
    }
}
