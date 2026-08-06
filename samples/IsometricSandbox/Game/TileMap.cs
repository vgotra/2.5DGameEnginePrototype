using System.Numerics;
using System.Runtime.CompilerServices;

namespace IsometricSandbox.Game;

public sealed class TileMap
{
    private readonly byte[] _tiles;
    private readonly Vector2[] _centers;
    public int Width { get; }
    public int Height { get; }
    public float TileWidth { get; }
    public float TileHeight { get; }

    public TileMap(int width = 20, int height = 20, float tileWidth = 64, float tileHeight = 32)
    {
        Width = width; Height = height; TileWidth = tileWidth; TileHeight = tileHeight;
        _tiles = new byte[width * height];
        _centers = new Vector2[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) { _tiles[y * width + x] = (byte)TileType.Floor; _centers[y * width + x] = new(x + 0.5f, y + 0.5f); }
        _tiles[(height / 2) * width + width / 2] = (byte)TileType.Goal;
    }

    public void LoadLayout(string[] rows)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char c = rows[y][x];
                _tiles[y * Width + x] = (byte)CharToType(c);
            }
    }

    private static TileType CharToType(char c) => c switch
    {
        'G' => TileType.Floor,
        'T' => TileType.Tree,
        'W' => TileType.Water,
        'F' => TileType.Bonfire,
        '#' => TileType.Wall,
        _ => TileType.Floor,
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInside(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TileType Get(int x, int y) => IsInside(x, y) ? (TileType)_tiles[y * Width + x] : TileType.Blocked;
    public void SetTile(int x, int y, TileType type)
    {
        if (!IsInside(x, y)) return;
        _tiles[y * Width + x] = (byte)type;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkable(int x, int y)
    {
        if (!IsInside(x, y)) return false;
        TileType type = (TileType)_tiles[y * Width + x];
        return type is TileType.Floor or TileType.Goal;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TileToWorld(int x, int y) => IsInside(x, y) ? _centers[y * Width + x] : new(x + 0.5f, y + 0.5f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanOccupy(Vector2 position, float radius)
    {
        int minX = (int)MathF.Floor(position.X - radius), maxX = (int)MathF.Floor(position.X + radius);
        int minY = (int)MathF.Floor(position.Y - radius), maxY = (int)MathF.Floor(position.Y + radius);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                if (IsWalkable(x, y)) continue;
                float closestX = Math.Clamp(position.X, x, x + 1f), closestY = Math.Clamp(position.Y, y, y + 1f);
                float dx = position.X - closestX, dy = position.Y - closestY;
                if (dx * dx + dy * dy < radius * radius) return false;
            }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TryMove(Vector2 position, Vector2 desired, float radius)
    {
        Vector2 result = position;
        if (CanOccupy(new(desired.X, position.Y), radius)) result.X = desired.X;
        if (CanOccupy(new(result.X, desired.Y), radius)) result.Y = desired.Y;
        return result;
    }
}
