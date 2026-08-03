using System.Numerics;
using Engine.Mathematics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public static class RenderExtractionSystem
{
    /// <summary>Pixel width of the black border overdraw behind each tile/player shape.</summary>
    public const float BorderWidth = 2f;

    public static int ExtractMapSprites(TileMap map, IsometricCamera camera, Span<SpritePacket> sprites)
    {
        Vector4 white = new(1, 1, 1, 1);
        Vector4 black = new(0, 0, 0, 1);
        ShapeKind shape = camera.Isometric ? ShapeKind.Diamond : ShapeKind.Box;
        float tileWidth = map.TileWidth;
        float tileHeight = camera.Isometric ? map.TileHeight : map.TileWidth;
        // Viewport culling: reject tiles whose on-screen bounding box lies entirely outside the
        // viewport. The AABB (half-extents + border) is valid for both the iso diamond and the
        // flat box, so off-screen tiles never reach the GPU. This is the culling the performance
        // budget documents; for maps larger than the viewport it bounds extraction work.
        float halfWidth = tileWidth * 0.5f + BorderWidth;
        float halfHeight = tileHeight * 0.5f + BorderWidth;
        Vector2 viewport = camera.Viewport;
        int written = 0;
        for (int y = 0; y < map.Height && written + 2 <= sprites.Length; y++)
        for (int x = 0; x < map.Width && written + 2 <= sprites.Length; x++)
        {
            Vector2 screen = camera.WorldToScreen(map.TileToWorld(x, y), map);
            if (screen.X + halfWidth < 0 || screen.X - halfWidth > viewport.X ||
                screen.Y + halfHeight < 0 || screen.Y - halfHeight > viewport.Y)
                continue;
            int sortKey = y * map.Width + x;
            sprites[written++] = new SpritePacket(screen, new(tileWidth + BorderWidth * 2, tileHeight + BorderWidth * 2), black, default, default, sortKey, shape);
            sprites[written++] = new SpritePacket(screen, new(tileWidth, tileHeight), white, default, default, sortKey, shape);
        }
        return written;
    }
}
