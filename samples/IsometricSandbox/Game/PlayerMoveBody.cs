using System.Numerics;
using Engine.App;
using Engine.Core;
using Engine.Ecs;

namespace IsometricSandbox.Game;

public struct PlayerMoveBody : IForEach<Position, Velocity, PlayerState, PlayerMoveBody>
{
    public TileMap Map;
    public Vector2 Direction;
    public bool JumpRequested;
    public float DeltaSeconds;

    public static void Execute(ref PlayerMoveBody body, EntityId entity, ref Position position, ref Velocity velocity, ref PlayerState state)
    {
        if (body.Direction.LengthSquared() > 0) state.Facing = Vector2.Normalize(body.Direction);
        if (body.JumpRequested && !state.IsJumping) TryStartJump(ref body, ref position, ref state);
        if (state.IsJumping)
        {
            state.JumpTime = Math.Min(SampleConfig.JumpDuration, state.JumpTime + body.DeltaSeconds);
            position.Value = Vector2.Lerp(state.JumpStart, state.JumpTarget, state.JumpTime / SampleConfig.JumpDuration);
            velocity.Value = Vector2.Zero;
        }
        else
        {
            velocity.Value = body.Direction * SampleConfig.PlayerSpeed;
        }
    }

    private static void TryStartJump(ref PlayerMoveBody body, ref Position position, ref PlayerState state)
    {
        Vector2 candidate = position.Value + state.Facing * 2f;
        if (!body.Map.CanOccupy(candidate, SampleConfig.PlayerRadius)) return;
        state.JumpStart = position.Value;
        state.JumpTarget = candidate;
        state.JumpTime = 0;
    }
}
