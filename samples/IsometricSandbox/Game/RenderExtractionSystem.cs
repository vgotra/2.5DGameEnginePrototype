using System.Numerics;
using Engine.Mathematics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public static class RenderExtractionSystem
{
    public static int ExtractMapSprites(TileMap map, IsometricCamera camera, Span<SpritePacket> sprites)
    {
        const float border = 2f;
        Vector4 white = new(1, 1, 1, 1);
        Vector4 black = new(0, 0, 0, 1);
        ShapeKind shape = camera.Isometric ? ShapeKind.Diamond : ShapeKind.Box;
        float tileWidth = map.TileWidth;
        float tileHeight = camera.Isometric ? map.TileHeight : map.TileWidth;
        int written = 0;
        for (int y = 0; y < map.Height && written + 2 <= sprites.Length; y++)
        for (int x = 0; x < map.Width && written + 2 <= sprites.Length; x++)
        {
            Vector2 screen = camera.WorldToScreen(map.TileToWorld(x, y), map);
            int sortKey = y * map.Width + x;
            sprites[written++] = new SpritePacket(screen, new(tileWidth + border * 2, tileHeight + border * 2), black, default, default, sortKey, shape);
            sprites[written++] = new SpritePacket(screen, new(tileWidth, tileHeight), white, default, default, sortKey, shape);
        }
        return written;
    }
}
