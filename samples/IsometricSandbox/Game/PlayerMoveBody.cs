using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

public struct PlayerMoveBody : IQueryAction<Position, Velocity, PlayerState, PlayerMoveBody>
{
    public TerrainSurface Map;
    public Vector2 Direction;
    public bool JumpRequested;
    public float DeltaSeconds;

    public static void Execute(ref PlayerMoveBody body, Entity entity, ref Position position, ref Velocity velocity, ref PlayerState state)
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
            Vector2 direction = body.Direction;
            if (direction.LengthSquared() > 1f)
                direction = Vector2.Normalize(direction);
            velocity.Value = direction * SampleConfig.PlayerSpeed;
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
