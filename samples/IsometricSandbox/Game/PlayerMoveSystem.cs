using System.Numerics;
using Engine.App;
using Engine.Core;
using Engine.Ecs;
using World = Engine.Ecs.World;
using Engine.Platform;

namespace IsometricSandbox.Game;

// The archer's movement system. Reads WASD/arrow input and the Space edge,
// then writes the desired velocity (or eases a jump into place directly).
// Actual collision-aware movement happens later in IntegrateSystem.
public sealed class PlayerMoveSystem(TileMap map, IInputState input) : ISystem
{
    public EntityId Player { get; set; }

    public ComponentAccess Access => ComponentAccess.Write<Velocity, PlayerState>();

    public void Update(World world, float deltaSeconds)
    {
        Vector2 direction = ReadMovement();
        bool jumpRequested = input.WasPressed(GameKey.Space);
        ref Position position = ref world.Get<Position>(Player);
        ref Velocity velocity = ref world.Get<Velocity>(Player);
        ref PlayerState state = ref world.Get<PlayerState>(Player);
        PlayerMoveBody body = new()
        {
            Map = map,
            Direction = direction,
            JumpRequested = jumpRequested,
            DeltaSeconds = deltaSeconds,
        };
        PlayerMoveBody.Execute(ref body, Player, ref position, ref velocity, ref state);
    }

    // Maps WASD/arrow keys to a movement direction.
    private Vector2 ReadMovement()
    {
        float right = (input.IsDown(GameKey.Right) ? 1 : 0) - (input.IsDown(GameKey.Left) ? 1 : 0);
        float down = (input.IsDown(GameKey.Down) ? 1 : 0) - (input.IsDown(GameKey.Up) ? 1 : 0);
        return new Vector2(right, down);
    }
}
