using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public sealed class IsometricProjection : ICameraProjection
{
    public static readonly IsometricProjection Instance = new();

    private IsometricProjection() { }

    public GameMode Mode => GameMode.Isometric;
    public ShapeKind TileShape => ShapeKind.Diamond;
    public float GetTileHeight(TileGrid grid) => grid.TileHeight;

    public ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TileGrid grid)
    {
        float scaleX = grid.TileWidth * 0.5f * zoom;
        float scaleY = grid.TileHeight * 0.5f * zoom;
        return new ScreenTransform(
            viewport.X * 0.5f - (position.X - position.Y) * scaleX,
            viewport.Y * 0.5f - (position.X + position.Y) * scaleY,
            scaleX, scaleY, -scaleX, scaleY);
    }

    public Vector2 ClampToMap(Vector2 target, TileGrid grid, Vector2 viewport)
    {
        float diff = target.X - target.Y;
        float sum = target.X + target.Y;
        float dLow = grid.Width - 1f - viewport.X / grid.TileWidth;
        float dHigh = viewport.X / grid.TileWidth - (grid.Height - 1f);
        float sLow = grid.Width + grid.Height - 1f - viewport.Y / grid.TileHeight;
        float sHigh = 1f + viewport.Y / grid.TileHeight;
        if (dLow <= dHigh) diff = 0f;
        else diff = Math.Clamp(diff, 2f - grid.Height, grid.Width - 2f);
        if (sLow <= sHigh) sum = (grid.Width + grid.Height) * 0.5f;
        else sum = Math.Clamp(sum, 2f, grid.Width + grid.Height - 2f);
        return new(
            Math.Clamp((sum + diff) * 0.5f, 1f, grid.Width - 1f),
            Math.Clamp((sum - diff) * 0.5f, 1f, grid.Height - 1f));
    }
}
