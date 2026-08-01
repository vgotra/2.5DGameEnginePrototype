using System.Numerics;
using Engine.Mathematics;

namespace IsometricSandbox.Game;

public sealed class IsometricCamera
{
    public Vector2 Position { get; private set; }
    public Vector2 Viewport { get; private set; }
    public float Zoom { get; set; } = 1f;
    public bool Isometric { get; set; } = true;
    public IsometricCamera(Vector2 viewport) => Viewport = viewport;
    public void Resize(Vector2 viewport) => Viewport = viewport;
    public void Follow(Vector2 target, TileMap map)
    {
        Vector2 position = target;
        if (Isometric)
        {
            // In isometric space the screen axes map to world (x-y) and (x+y).
            // Clamp the camera so the map bounding box stays inside the viewport;
            // when the map fits on an axis it is centered there, otherwise the
            // camera follows the player within the map's on-screen range.
            float diff = position.X - position.Y;
            float sum = position.X + position.Y;
            float dLow = map.Width - 1f - Viewport.X / map.TileWidth;
            float dHigh = Viewport.X / map.TileWidth - (map.Height - 1f);
            float sLow = map.Width + map.Height - 1f - Viewport.Y / map.TileHeight;
            float sHigh = 1f + Viewport.Y / map.TileHeight;
            if (dLow <= dHigh) diff = 0f;
            else diff = Math.Clamp(diff, 2f - map.Height, map.Width - 2f);
            if (sLow <= sHigh) sum = (map.Width + map.Height) * 0.5f;
            else sum = Math.Clamp(sum, 2f, map.Width + map.Height - 2f);
            position = new(Math.Clamp((sum + diff) * 0.5f, 1f, map.Width - 1f), Math.Clamp((sum - diff) * 0.5f, 1f, map.Height - 1f));
        }
        else
        {
            // Cartesian axes: the map spans tiles 0..Width-1 / 0..Height-1 at
            // TileWidth px per axis. Center an axis when the map fits the viewport.
            float xLow = map.Width - 0.5f - Viewport.X / (map.TileWidth * 2f);
            float xHigh = 0.5f + Viewport.X / (map.TileWidth * 2f);
            float yLow = map.Height - 0.5f - Viewport.Y / (map.TileWidth * 2f);
            float yHigh = 0.5f + Viewport.Y / (map.TileWidth * 2f);
            position.X = xLow <= xHigh ? map.Width * 0.5f : Math.Clamp(position.X, 1f, map.Width - 1f);
            position.Y = yLow <= yHigh ? map.Height * 0.5f : Math.Clamp(position.Y, 1f, map.Height - 1f);
        }
        Position = position;
    }
    public Vector2 WorldToScreen(Vector2 world, TileMap map)
    {
        if (!Isometric) return ((world - Position) * Zoom * map.TileWidth) + Viewport * 0.5f;
        return (IsometricMath.WorldToScreen(world - Position, map.TileWidth, map.TileHeight) * Zoom) + Viewport * 0.5f;
    }
}
