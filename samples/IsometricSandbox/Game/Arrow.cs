using System.Numerics;

namespace IsometricSandbox.Game;

public struct Arrow
{
    public Vector2 Position;
    public Vector2 Direction;
    public float Speed;
    public float Lifetime;

    public const float Radius = 0.15f;
}
