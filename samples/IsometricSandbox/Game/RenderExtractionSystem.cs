using System.Numerics;
using Engine.Mathematics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public static class RenderExtractionSystem
{
    public static int ExtractMap(TileMap map, IsometricCamera camera, Span<ShapeVertex> vertices)
    {
        int written = 0;
        for (int y = 0; y < map.Height && written + 6 <= vertices.Length; y++)
        for (int x = 0; x < map.Width && written + 6 <= vertices.Length; x++)
        {
            Vector2 screen = camera.WorldToScreen(map.TileToWorld(x, y), map);
            Vector4 color = map.IsWalkable(x, y) ? ((x + y) & 1) == 0 ? new(0.25f, 0.42f, 0.3f, 1) : new(0.2f, 0.34f, 0.25f, 1) : new(0.12f, 0.12f, 0.15f, 1);
            written += GeneratedGeometry.AppendDiamond(vertices[written..], screen, map.TileWidth, map.TileHeight, color);
        }
        return written;
    }
}
