using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.App;

public class TerrainSurface
{
    private readonly float[] _heights;
    private readonly byte[] _surfaces;

    public int Width { get; }
    public int Height { get; }
    public float SampleSpacing { get; }
    public float TileWidth { get; }
    public float TileHeight { get; }
    public Vector2 Bounds => new(Width * SampleSpacing, Height * SampleSpacing);

    public TerrainSurface(int width, int height, float sampleSpacing = 1f, float tileWidth = 64f, float tileHeight = 32f, int seed = 1337)
    {
        if (width < 2 || height < 2) throw new ArgumentOutOfRangeException(nameof(width));
        if (sampleSpacing <= 0f) throw new ArgumentOutOfRangeException(nameof(sampleSpacing));
        Width = width;
        Height = height;
        SampleSpacing = sampleSpacing;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        _heights = new float[width * height];
        _surfaces = new byte[width * height];
        Generate(seed);
    }

    public float SampleHeight(Vector2 position)
    {
        float x = Math.Clamp(position.X / SampleSpacing, 0f, Width - 1f);
        float y = Math.Clamp(position.Y / SampleSpacing, 0f, Height - 1f);
        int x0 = (int)x;
        int y0 = (int)y;
        int x1 = Math.Min(x0 + 1, Width - 1);
        int y1 = Math.Min(y0 + 1, Height - 1);
        float tx = x - x0;
        float ty = y - y0;
        float top = _heights[y0 * Width + x0] + (_heights[y0 * Width + x1] - _heights[y0 * Width + x0]) * tx;
        float bottom = _heights[y1 * Width + x0] + (_heights[y1 * Width + x1] - _heights[y1 * Width + x0]) * tx;
        return top + (bottom - top) * ty;
    }

    public TileType SampleSurface(Vector2 position)
    {
        int x = Math.Clamp((int)MathF.Floor(position.X / SampleSpacing), 0, Width - 1);
        int y = Math.Clamp((int)MathF.Floor(position.Y / SampleSpacing), 0, Height - 1);
        return (TileType)_surfaces[y * Width + x];
    }

    public void SetHeight(int x, int y, float height)
    {
        if (!IsInside(x, y)) throw new ArgumentOutOfRangeException();
        _heights[y * Width + x] = height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInside(Vector2 position, float radius = 0f)
        => position.X >= radius && position.Y >= radius && position.X < Width * SampleSpacing - radius && position.Y < Height * SampleSpacing - radius;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInside(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanOccupy(Vector2 position, float radius)
    {
        if (!IsInside(position, radius)) return false;
        int minX = (int)MathF.Floor(position.X - radius);
        int maxX = (int)MathF.Floor(position.X + radius);
        int minY = (int)MathF.Floor(position.Y - radius);
        int maxY = (int)MathF.Floor(position.Y + radius);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                if (IsWalkable(x, y)) continue;
                float closestX = Math.Clamp(position.X, x, x + 1f);
                float closestY = Math.Clamp(position.Y, y, y + 1f);
                float dx = position.X - closestX;
                float dy = position.Y - closestY;
                if (dx * dx + dy * dy < radius * radius) return false;
            }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ResolveMove(Vector2 position, Vector2 desired, float radius)
    {
        Vector2 result = position;
        if (CanOccupy(new(desired.X, position.Y), radius)) result.X = desired.X;
        if (CanOccupy(new(result.X, desired.Y), radius)) result.Y = desired.Y;
        return result;
    }


    public void SetTile(int x, int y, TileType type)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        _surfaces[y * Width + x] = (byte)type;
    }

    public void LoadLayout(string[] rows)
    {
        for (int y = 0; y < Math.Min(Height, rows.Length); y++)
            for (int x = 0; x < Math.Min(Width, rows[y].Length); x++)
                SetTile(x, y, rows[y][x] switch
                {
                    'T' => TileType.Tree,
                    'W' => TileType.Water,
                    'F' => TileType.Bonfire,
                    '#' => TileType.Wall,
                    _ => TileType.Floor,
                });
    }

    public Vector2 TileToWorld(int x, int y) => new(x + 0.5f, y + 0.5f);

    public bool IsWalkable(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return false;
        TileType surface = (TileType)_surfaces[y * Width + x];
        return surface is TileType.Floor or TileType.Goal or TileType.Bonfire;
    }

    public Vector2 TryMove(Vector2 position, Vector2 desired, float radius) => ResolveMove(position, desired, radius);

    private void Generate(int seed)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                uint value = (uint)(seed * 747796405 + x * 2891336453u + y * 1181783497u);
                value ^= value >> 16;
                float noise = (value & 1023) / 1023f;
                _heights[y * Width + x] = noise * 0.75f;
                _surfaces[y * Width + x] = (byte)(noise > 0.82f ? TileType.Tree : TileType.Floor);
            }
    }
}
