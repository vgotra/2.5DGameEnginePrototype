using System.Numerics;

namespace IsometricSandbox.Game;

public struct ArrowProjectile
{
    public Vector2 Direction;
    public float Speed;
    public float Lifetime;

    public const float Radius = SampleConfig.ArrowRadius;
}
