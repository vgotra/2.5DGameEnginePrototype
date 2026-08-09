namespace Engine.App;

public sealed class TileGrid
{
    private readonly byte[] _tiles;

    public int Width { get; }
    public int Height { get; }
    public float TileWidth { get; }
    public float TileHeight { get; }

    public TileGrid(int width, int height, float tileWidth, float tileHeight, byte[] tiles)
    {
        if (tiles.Length < width * height) throw new ArgumentException("Tile data smaller than grid dimensions.", nameof(tiles));
        Width = width;
        Height = height;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        _tiles = tiles;
    }

    public byte Get(int x, int y) => _tiles[y * Width + x];

    public void Set(int x, int y, byte type) => _tiles[y * Width + x] = type;
}
