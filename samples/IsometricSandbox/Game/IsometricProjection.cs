using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public sealed class IsometricProjection : ICameraProjection
{
    public static readonly IsometricProjection Instance = new();

    private IsometricProjection() { }

    public GameMode Mode => GameMode.Isometric;
    public ShapeKind TileShape => ShapeKind.Diamond;
    public float GetTileHeight(TileMap map) => map.TileHeight;

    public ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TileMap map)
    {
        float scaleX = map.TileWidth * 0.5f * zoom;
        float scaleY = map.TileHeight * 0.5f * zoom;
        return new ScreenTransform(
            viewport.X * 0.5f - (position.X - position.Y) * scaleX,
            viewport.Y * 0.5f - (position.X + position.Y) * scaleY,
            scaleX, scaleY, -scaleX, scaleY);
    }

    public Vector2 ClampToMap(Vector2 target, TileMap map, Vector2 viewport)
    {
        float diff = target.X - target.Y;
        float sum = target.X + target.Y;
        float dLow = map.Width - 1f - viewport.X / map.TileWidth;
        float dHigh = viewport.X / map.TileWidth - (map.Height - 1f);
        float sLow = map.Width + map.Height - 1f - viewport.Y / map.TileHeight;
        float sHigh = 1f + viewport.Y / map.TileHeight;
        if (dLow <= dHigh) diff = 0f;
        else diff = Math.Clamp(diff, 2f - map.Height, map.Width - 2f);
        if (sLow <= sHigh) sum = (map.Width + map.Height) * 0.5f;
        else sum = Math.Clamp(sum, 2f, map.Width + map.Height - 2f);
        return new(
            Math.Clamp((sum + diff) * 0.5f, 1f, map.Width - 1f),
            Math.Clamp((sum - diff) * 0.5f, 1f, map.Height - 1f));
    }
}
