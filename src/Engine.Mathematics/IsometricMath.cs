using System.Numerics;

namespace Engine.Mathematics;

public static class IsometricMath
{
    public static Vector2 WorldToScreen(Vector2 world, float tileWidth, float tileHeight)
        => new((world.X - world.Y) * tileWidth * 0.5f, (world.X + world.Y) * tileHeight * 0.5f);

    public static Vector2 ScreenToWorld(Vector2 screen, float tileWidth, float tileHeight)
    {
        float x = screen.X / (tileWidth * 0.5f);
        float y = screen.Y / (tileHeight * 0.5f);
        return new((x + y) * 0.5f, (y - x) * 0.5f);
    }
}
