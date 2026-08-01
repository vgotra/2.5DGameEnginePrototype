using System.Numerics;

namespace IsometricSandbox.Game;

public static class MovementSystem
{
    public static Vector2 Move(TileMap map, Vector2 position, Vector2 input, float speed, float radius, float deltaSeconds)
    {
        if (input.LengthSquared() > 1f) input = Vector2.Normalize(input);
        return map.TryMove(position, position + input * speed * deltaSeconds, radius);
    }
}
