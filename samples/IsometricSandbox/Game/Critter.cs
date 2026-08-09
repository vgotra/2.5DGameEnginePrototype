using System.Numerics;

namespace IsometricSandbox.Game;

public struct Critter
{
    public AnimalSpecies Species;
    public float Speed;
    public float Radius;
    public Vector2 WanderTarget;
}
