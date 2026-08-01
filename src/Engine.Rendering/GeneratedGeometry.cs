using System.Numerics;

namespace Engine.Rendering;

public readonly record struct ShapeVertex(Vector2 Position, Vector4 Color);

public static class GeneratedGeometry
{
    public static int AppendDiamond(Span<ShapeVertex> destination, Vector2 center, float width, float height, Vector4 color)
    {
        if (destination.Length < 6) return 0;
        Vector2 top = center + new Vector2(0, -height * 0.5f);
        Vector2 right = center + new Vector2(width * 0.5f, 0);
        Vector2 bottom = center + new Vector2(0, height * 0.5f);
        Vector2 left = center + new Vector2(-width * 0.5f, 0);
        destination[0] = new(top, color); destination[1] = new(right, color); destination[2] = new(bottom, color);
        destination[3] = new(top, color); destination[4] = new(bottom, color); destination[5] = new(left, color);
        return 6;
    }
}
