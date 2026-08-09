using System.Numerics;
using Engine.Rendering;
using Engine.Threading;

namespace IsometricSandbox.Game;

// Reusable, allocation-free closure for parallel tile extraction: fields are
// re-armed each frame and the cached RunBody delegate is handed to
// JobSystem.ScheduleFor, so steady-state dispatch allocates nothing.
public sealed class TileExtractionDispatch
{
    private readonly Action<int, int> _runBody;

    private TileMap _map = null!;
    private ScreenTransform _transform;
    private Vector2 _viewport;
    private ShapeKind _shape;
    private float _tileWidth;
    private float _tileHeight;
    private int _rowsPerBand;
    private TextureLibrary? _textures;
    private SpritePacket[][] _bands = null!;
    private int[] _counts = null!;
    private Random[] _flickers = null!;

    public TileExtractionDispatch()
    {
        _runBody = Run;
    }

    public JobHandle Schedule(
        JobSystem jobs,
        TileMap map,
        IsometricCamera camera,
        TextureLibrary? textures,
        SpritePacket[][] bands,
        int[] counts,
        Random[] flickers,
        int bandCount,
        int rowsPerBand)
    {
        _map = map;
        _transform = camera.GetScreenTransform(map);
        _viewport = camera.Viewport;
        _shape = camera.Projection.TileShape;
        _tileWidth = map.TileWidth;
        _tileHeight = camera.Projection.GetTileHeight(map);
        _textures = textures;
        _bands = bands;
        _counts = counts;
        _flickers = flickers;
        _rowsPerBand = rowsPerBand;
        return jobs.ScheduleFor(bandCount, 1, _runBody);
    }

    private void Run(int lo, int hi)
    {
        for (int band = lo; band < hi; band++)
        {
            int yStart = band * _rowsPerBand;
            int yEnd = Math.Min(yStart + _rowsPerBand, _map.Height);
            _counts[band] = RenderExtractionSystem.ExtractTileRange(
                _map, in _transform, _viewport, _shape, _tileWidth, _tileHeight,
                yStart, yEnd, _bands[band], _textures, _flickers[band]);
        }
    }
}
