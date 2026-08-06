using System.Numerics;

namespace IsometricSandbox.Game;

public struct Animal
{
    public Vector2 Position;
    public AnimalSpecies Species;
    public float Speed;
    public float Radius;
    public Vector2 WanderTarget;
    public bool Alive;
    public float RespawnTimer;
}
