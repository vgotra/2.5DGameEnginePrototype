using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public interface ICameraProjection
{
    GameMode Mode { get; }
    ShapeKind TileShape { get; }
    float GetTileHeight(TileGrid grid);
    ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TileGrid grid);
    Vector2 ClampToMap(Vector2 target, TileGrid grid, Vector2 viewport);
}
