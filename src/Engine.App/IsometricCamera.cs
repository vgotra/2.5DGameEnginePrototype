using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.App;

public sealed class IsometricCamera
{
    public Vector2 Position { get; private set; }
    public Vector2 Viewport { get; private set; }
    public float Zoom { get; set; } = 1f;
    public GameMode Mode { get; set; } = GameMode.Isometric;

    public IsometricCamera(Vector2 viewport) => Viewport = viewport;

    public void Resize(Vector2 viewport) => Viewport = viewport;

    public ICameraProjection Projection => Mode == GameMode.Isometric ? IsometricProjection.Instance : OrthographicProjection.Instance;

    public void Follow(Vector2 target, TileGrid grid) => Position = Projection.ClampToMap(target, grid, Viewport);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScreenTransform GetScreenTransform(TileGrid grid) => Projection.GetTransform(Viewport, Position, Zoom, grid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 WorldToScreen(Vector2 world, TileGrid grid)
        => GetScreenTransform(grid).ToScreen(world.X, world.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ScreenToWorld(Vector2 screen, TileGrid grid)
    {
        ScreenTransform transform = GetScreenTransform(grid);
        float dx = screen.X - transform.OriginX;
        float dy = screen.Y - transform.OriginY;
        float det = transform.ScaleX * transform.ScaleY - transform.ShearX * transform.ShearY;
        if (det == 0f) return Position;
        float worldX = (dx * transform.ScaleY - dy * transform.ShearX) / det;
        float worldY = (transform.ScaleX * dy - dx * transform.ShearY) / det;
        return new Vector2(worldX, worldY);
    }
}
