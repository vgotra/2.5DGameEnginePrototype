using System.Numerics;

namespace Engine.App;

public static class IsometricProjection
{
    public static ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TerrainSurface terrain)
    {
        float scaleX = terrain.TileWidth * 0.5f * zoom;
        float scaleY = terrain.TileHeight * 0.5f * zoom;
        return new ScreenTransform(
            viewport.X * 0.5f - (position.X - position.Y) * scaleX,
            viewport.Y * 0.5f - (position.X + position.Y) * scaleY,
            scaleX, scaleY, -scaleX, scaleY);
    }

    public static Vector2 ClampToMap(Vector2 target, TerrainSurface terrain, Vector2 viewport)
    {
        float width = terrain.Width;
        float height = terrain.Height;
        float diff = Math.Clamp(target.X - target.Y, 2f - height, width - 2f);
        float sum = Math.Clamp(target.X + target.Y, 2f, width + height - 2f);
        return new(Math.Clamp((sum + diff) * 0.5f, 1f, width - 1f), Math.Clamp((sum - diff) * 0.5f, 1f, height - 1f));
    }
}
