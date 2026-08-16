using System.Numerics;
using IsometricSandbox.Game.Configuration;

namespace IsometricSandbox.Game.Gameplay.Components;

public struct ArrowProjectile
{
    public Vector2 Direction;
    public float Speed;
    public float Lifetime;

    public const float Radius = SampleConfig.ArrowRadius;
}
