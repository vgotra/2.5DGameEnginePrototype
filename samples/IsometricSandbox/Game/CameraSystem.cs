using System.Numerics;
using Engine.Mathematics;

namespace IsometricSandbox.Game;

public sealed class IsometricCamera
{
    public Vector2 Position { get; private set; }
    public Vector2 Viewport { get; private set; }
    public float Zoom { get; set; } = 1f;
    public IsometricCamera(Vector2 viewport) => Viewport = viewport;
    public void Resize(Vector2 viewport) => Viewport = viewport;
    public void Follow(Vector2 target, TileMap map)
    {
        Vector2 position = target;
        position.X = Math.Clamp(position.X, 1f, map.Width - 1f);
        position.Y = Math.Clamp(position.Y, 1f, map.Height - 1f);
        Position = position;
    }
    public Vector2 WorldToScreen(Vector2 world, TileMap map)
        => (IsometricMath.WorldToScreen(world - Position, map.TileWidth, map.TileHeight) * Zoom) + Viewport * 0.5f;
}
