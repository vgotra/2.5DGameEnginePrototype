using System.Numerics;
using IsometricSandbox.Game.Configuration;

namespace IsometricSandbox.Game.Gameplay.Components;

public struct PlayerState
{
    public Vector2 Facing;
    public Vector2 JumpStart;
    public Vector2 JumpTarget;
    public float JumpTime;
    public Vector2 AimTarget;
    public bool PendingShot;

    public bool IsJumping => JumpTime < SampleConfig.JumpDuration;

    public float JumpHeight
    {
        get
        {
            float progress = Math.Clamp(JumpTime / SampleConfig.JumpDuration, 0, 1);
            return progress >= 1 ? 0 : MathF.Sin(progress * MathF.PI) * SampleConfig.JumpHeight;
        }
    }

    public static PlayerState At(Vector2 start) => new()
    {
        Facing = new Vector2(0, 1),
        JumpStart = start,
        JumpTarget = start,
        JumpTime = SampleConfig.JumpDuration,
        AimTarget = start,
        PendingShot = false,
    };
}
