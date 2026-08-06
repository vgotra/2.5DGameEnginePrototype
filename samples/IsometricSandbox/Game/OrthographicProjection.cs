using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public sealed class OrthographicProjection : ICameraProjection
{
    public static readonly OrthographicProjection Instance = new();

    private OrthographicProjection() { }

    public GameMode Mode => GameMode.TopDown;
    public ShapeKind TileShape => ShapeKind.Box;
    public float GetTileHeight(TileMap map) => map.TileWidth;

    public ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TileMap map)
    {
        float scale = zoom * map.TileWidth;
        return new ScreenTransform(
            viewport.X * 0.5f - position.X * scale,
            viewport.Y * 0.5f - position.Y * scale,
            scale, scale, 0f, 0f);
    }

    public Vector2 ClampToMap(Vector2 target, TileMap map, Vector2 viewport)
    {
        float xLow = map.Width - 0.5f - viewport.X / (map.TileWidth * 2f);
        float xHigh = 0.5f + viewport.X / (map.TileWidth * 2f);
        float yLow = map.Height - 0.5f - viewport.Y / (map.TileWidth * 2f);
        float yHigh = 0.5f + viewport.Y / (map.TileWidth * 2f);
        float x = xLow <= xHigh ? map.Width * 0.5f : Math.Clamp(target.X, 1f, map.Width - 1f);
        float y = yLow <= yHigh ? map.Height * 0.5f : Math.Clamp(target.Y, 1f, map.Height - 1f);
        return new(x, y);
    }
}
