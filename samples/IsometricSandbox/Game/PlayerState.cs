using System.Numerics;

namespace IsometricSandbox.Game;

public struct PlayerState
{
    public Vector2 Facing;
    public Vector2 JumpStart;
    public Vector2 JumpTarget;
    public float JumpTime;
    public Vector2 AimTarget;
    public bool PendingShot;

    public bool IsJumping => JumpTime < SampleConfig.JumpDuration;

    // Render lift in screen pixels: rises to a peak mid-jump, then lands.
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
