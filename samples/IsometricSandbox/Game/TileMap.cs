using System.Numerics;

namespace IsometricSandbox.Game;

public enum TileType : byte { Floor, Blocked, Goal }

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

    public bool IsInside(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
    public TileType Get(int x, int y) => IsInside(x, y) ? (TileType)_tiles[y * Width + x] : TileType.Blocked;
    public void SetTile(int x, int y, TileType type)
    {
        if (!IsInside(x, y)) return;
        _tiles[y * Width + x] = (byte)type;
    }
    public bool IsWalkable(int x, int y) => !IsInside(x, y) || _tiles[y * Width + x] != (byte)TileType.Blocked;
    public Vector2 TileToWorld(int x, int y) => IsInside(x, y) ? _centers[y * Width + x] : new(x + 0.5f, y + 0.5f);

    public bool CanOccupy(Vector2 position, float radius)
    {
        // Check only cells touched by the player's circle. The closest-point test
        // also prevents clipping through blocked tile corners.
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

    public Vector2 TryMove(Vector2 position, Vector2 desired, float radius)
    {
        // Resolve X and Y independently so blocked movement slides along obstacles.
        Vector2 result = position;
        if (CanOccupy(new(desired.X, position.Y), radius)) result.X = desired.X;
        if (CanOccupy(new(result.X, desired.Y), radius)) result.Y = desired.Y;
        return result;
    }
}
