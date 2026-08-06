using System.Numerics;
using Engine.Rendering;

namespace IsometricSandbox.Game;

public interface ICameraProjection
{
    GameMode Mode { get; }
    ShapeKind TileShape { get; }
    float GetTileHeight(TileMap map);
    ScreenTransform GetTransform(Vector2 viewport, Vector2 position, float zoom, TileMap map);
    Vector2 ClampToMap(Vector2 target, TileMap map, Vector2 viewport);
}
