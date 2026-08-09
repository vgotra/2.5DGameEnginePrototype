using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public sealed class OrthographicProjection : ICameraProjection
{
    public static readonly OrthographicProjection Instance = new();

    private OrthographicProjection() { }

    public GameMode Mode => GameMode.TopDown;
    public ShapeKind TileShape => ShapeKind.Box;
    public float GetTileHeight(TileGrid grid) => grid.TileWidth;

    public ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TileGrid grid)
    {
        float scale = zoom * grid.TileWidth;
        return new ScreenTransform(
            viewport.X * 0.5f - position.X * scale,
            viewport.Y * 0.5f - position.Y * scale,
            scale, scale, 0f, 0f);
    }

    public Vector2 ClampToMap(Vector2 target, TileGrid grid, Vector2 viewport)
    {
        float xLow = grid.Width - 0.5f - viewport.X / (grid.TileWidth * 2f);
        float xHigh = 0.5f + viewport.X / (grid.TileWidth * 2f);
        float yLow = grid.Height - 0.5f - viewport.Y / (grid.TileWidth * 2f);
        float yHigh = 0.5f + viewport.Y / (grid.TileWidth * 2f);
        float x = xLow <= xHigh ? grid.Width * 0.5f : Math.Clamp(target.X, 1f, grid.Width - 1f);
        float y = yLow <= yHigh ? grid.Height * 0.5f : Math.Clamp(target.Y, 1f, grid.Height - 1f);
        return new(x, y);
    }
}
