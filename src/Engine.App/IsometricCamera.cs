using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.App;

public sealed class IsometricCamera(Vector2 viewport)
{
    public Vector2 Position { get; private set; }
    public Vector2 Viewport { get; private set; } = viewport;
    public float Zoom { get; set; } = 1f;
    public void Resize(Vector2 viewport) => Viewport = viewport;

    public void Follow(Vector2 target, TerrainSurface terrain) => Position = IsometricProjection.ClampToMap(target, terrain, Viewport);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScreenTransform GetScreenTransform(TerrainSurface terrain) => IsometricProjection.GetTransform(Viewport, Position, Zoom, terrain);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 WorldToScreen(Vector2 world, TerrainSurface terrain)
        => GetScreenTransform(terrain).ToScreen(world.X, world.Y) - new Vector2(0f, terrain.SampleHeight(world) * terrain.TileHeight * Zoom);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ScreenToWorld(Vector2 screen, TerrainSurface terrain)
    {
        ScreenTransform transform = GetScreenTransform(terrain);
        float dx = screen.X - transform.OriginX;
        float dy = screen.Y - transform.OriginY;
        float det = transform.ScaleX * transform.ScaleY - transform.ShearX * transform.ShearY;
        if (det == 0f) return Position;
        float worldX = (dx * transform.ScaleY - dy * transform.ShearX) / det;
        float worldY = (transform.ScaleX * dy - dx * transform.ShearY) / det;
        return new Vector2(worldX, worldY);
    }
}
