using System.Numerics;

namespace IsometricSandbox.Game;

public static class TileVisual
{
    public static Vector4 Color(TileType type) => type switch
    {
        TileType.Floor => new(0.36f, 0.66f, 0.29f, 1f),
        TileType.Tree => new(0.13f, 0.36f, 0.17f, 1f),
        TileType.Water => new(0.20f, 0.47f, 0.75f, 1f),
        TileType.Bonfire => new(0.86f, 0.43f, 0.12f, 1f),
        TileType.Wall => new(0.35f, 0.35f, 0.35f, 1f),
        TileType.Goal => new(0.95f, 0.84f, 0.25f, 1f),
        _ => new(1f, 1f, 1f, 1f),
    };

    public static string? TextureName(TileType type) => type switch
    {
        TileType.Floor => "grass",
        TileType.Tree => "tree",
        TileType.Water => "water",
        TileType.Bonfire => "bonfire",
        TileType.Wall => "wall",
        _ => null,
    };
}
