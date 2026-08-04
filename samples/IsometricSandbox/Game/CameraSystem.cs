using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Mathematics;

namespace IsometricSandbox.Game;

public readonly struct ScreenTransform
{
    public readonly float OriginX;
    public readonly float OriginY;
    public readonly float ScaleX;
    public readonly float ScaleY;
    public readonly bool Isometric;

    public ScreenTransform(float originX, float originY, float scaleX, float scaleY, bool isometric)
    {
        OriginX = originX;
        OriginY = originY;
        ScaleX = scaleX;
        ScaleY = scaleY;
        Isometric = isometric;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToScreen(float worldX, float worldY)
    {
        if (Isometric)
            return new(OriginX + (worldX - worldY) * ScaleX, OriginY + (worldX + worldY) * ScaleY);
        return new(OriginX + worldX * ScaleX, OriginY + worldY * ScaleY);
    }
}

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
            float xLow = map.Width - 0.5f - Viewport.X / (map.TileWidth * 2f);
            float xHigh = 0.5f + Viewport.X / (map.TileWidth * 2f);
            float yLow = map.Height - 0.5f - Viewport.Y / (map.TileWidth * 2f);
            float yHigh = 0.5f + Viewport.Y / (map.TileWidth * 2f);
            position.X = xLow <= xHigh ? map.Width * 0.5f : Math.Clamp(position.X, 1f, map.Width - 1f);
            position.Y = yLow <= yHigh ? map.Height * 0.5f : Math.Clamp(position.Y, 1f, map.Height - 1f);
        }
        Position = position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScreenTransform GetScreenTransform(TileMap map)
    {
        if (!Isometric)
        {
            float scale = Zoom * map.TileWidth;
            return new(
                Viewport.X * 0.5f - Position.X * scale,
                Viewport.Y * 0.5f - Position.Y * scale,
                scale, scale, false);
        }
        float scaleX = map.TileWidth * 0.5f * Zoom;
        float scaleY = map.TileHeight * 0.5f * Zoom;
        return new(
            Viewport.X * 0.5f - (Position.X - Position.Y) * scaleX,
            Viewport.Y * 0.5f - (Position.X + Position.Y) * scaleY,
            scaleX, scaleY, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 WorldToScreen(Vector2 world, TileMap map)
        => GetScreenTransform(map).ToScreen(world.X, world.Y);
}
