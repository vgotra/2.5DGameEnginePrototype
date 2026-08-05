using System.Numerics;
using System.Runtime.CompilerServices;

namespace IsometricSandbox.Game;

public readonly struct ScreenTransform
{
    public readonly float OriginX;
    public readonly float OriginY;
    public readonly float ScaleX;
    public readonly float ScaleY;
    public readonly float ShearX;
    public readonly float ShearY;

    public ScreenTransform(float originX, float originY, float scaleX, float scaleY, float shearX, float shearY)
    {
        OriginX = originX;
        OriginY = originY;
        ScaleX = scaleX;
        ScaleY = scaleY;
        ShearX = shearX;
        ShearY = shearY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToScreen(float worldX, float worldY)
        => new(OriginX + worldX * ScaleX + worldY * ShearX, OriginY + worldX * ShearY + worldY * ScaleY);
}

public sealed class IsometricCamera
{
    public Vector2 Position { get; private set; }
    public Vector2 Viewport { get; private set; }
    public float Zoom { get; set; } = 1f;
    public GameMode Mode { get; set; } = GameMode.Isometric;
    public IsometricCamera(Vector2 viewport) => Viewport = viewport;
    public void Resize(Vector2 viewport) => Viewport = viewport;

    public ICameraProjection Projection => Mode == GameMode.Isometric ? IsometricProjection.Instance : OrthographicProjection.Instance;

    public void Follow(Vector2 target, TileMap map) => Position = Projection.ClampToMap(target, map, Viewport);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScreenTransform GetScreenTransform(TileMap map) => Projection.GetTransform(Viewport, Position, Zoom, map);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 WorldToScreen(Vector2 world, TileMap map)
        => GetScreenTransform(map).ToScreen(world.X, world.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ScreenToWorld(Vector2 screen, TileMap map)
    {
        ScreenTransform transform = GetScreenTransform(map);
        float dx = screen.X - transform.OriginX;
        float dy = screen.Y - transform.OriginY;
        float det = transform.ScaleX * transform.ScaleY - transform.ShearX * transform.ShearY;
        if (det == 0f) return Position;
        float worldX = (dx * transform.ScaleY - dy * transform.ShearX) / det;
        float worldY = (transform.ScaleX * dy - dx * transform.ShearY) / det;
        return new Vector2(worldX, worldY);
    }
}
