using System.Numerics;
using Engine.App;
using System.Runtime.CompilerServices;

namespace IsometricSandbox.Game;

public static class MovementSystem
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Move(TerrainSurface map, Vector2 position, Vector2 input, float speed, float radius, float deltaSeconds)
    {
        if (input.LengthSquared() > 1f) input = Vector2.Normalize(input);
        return map.TryMove(position, position + input * speed * deltaSeconds, radius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 MoveVelocity(TerrainSurface map, Vector2 position, Vector2 velocity, float radius, float deltaSeconds)
        => map.TryMove(position, position + velocity * deltaSeconds, radius);
}
