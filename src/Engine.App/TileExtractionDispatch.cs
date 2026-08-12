using System.Numerics;
using Engine.Rendering;
using Engine.Threading;

namespace Engine.App;

public sealed class TileExtractionDispatch
{
    private readonly Action<int, int> _runBody;

    private TileGrid _grid = null!;
    private ScreenTransform _transform;
    private Vector2 _viewport;
    private ShapeKind _shape;
    private float _tileWidth;
    private float _tileHeight;
    private int _rowsPerBand;
    private ITileTextureProvider? _textures;
    private SpritePacket[][] _bands = null!;
    private int[] _counts = null!;
    private Random[] _flickers = null!;

    public TileExtractionDispatch() => _runBody = Run;

    public JobHandle Schedule(
        JobSystem jobs,
        TileGrid grid,
        IsometricCamera camera,
        ITileTextureProvider? textures,
        SpritePacket[][] bands,
        int[] counts,
        Random[] flickers,
        int bandCount,
        int rowsPerBand)
    {
        _grid = grid;
        _transform = camera.GetScreenTransform(grid);
        _viewport = camera.Viewport;
        _shape = camera.Projection.TileShape;
        _tileWidth = grid.TileWidth;
        _tileHeight = camera.Projection.GetTileHeight(grid);
        _textures = textures;
        _bands = bands;
        _counts = counts;
        _flickers = flickers;
        _rowsPerBand = rowsPerBand;
        return jobs.ParallelFor(bandCount, 1, _runBody);
    }

    private void Run(int lo, int hi)
    {
        for (int band = lo; band < hi; band++)
        {
            int yStart = band * _rowsPerBand;
            int yEnd = Math.Min(yStart + _rowsPerBand, _grid.Height);
            _counts[band] = SpriteExtraction.ExtractTileRange(
                _grid, in _transform, _viewport, _shape, _tileWidth, _tileHeight,
                yStart, yEnd, _bands[band], _textures, _flickers[band]);
        }
    }
}
