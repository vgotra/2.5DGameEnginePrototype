using System.Numerics;

namespace Engine.App;

public static class TileVisual
{
    public static Vector4 Color(TileType type) => type switch
    {
        TileType.Floor => new(0.50f, 0.72f, 0.32f, 1f),
        TileType.Tree => new(0.20f, 0.45f, 0.18f, 1f),
        TileType.Water => new(0.18f, 0.55f, 0.62f, 1f),
        TileType.Bonfire => new(1.00f, 0.72f, 0.10f, 1f),
        TileType.Wall => new(0.48f, 0.46f, 0.42f, 1f),
        TileType.Goal => new(0.95f, 0.84f, 0.25f, 1f),
        _ => new(1f, 1f, 1f, 1f),
    };

    public static Vector4 TextureTint(TileType type) => type switch
    {
        TileType.Floor => new(1.12f, 1.08f, 0.88f, 1f),
        TileType.Tree => new(1.05f, 1.08f, 0.92f, 1f),
        TileType.Water => new(0.85f, 1.10f, 1.12f, 1f),
        TileType.Bonfire => new(1.10f, 1.05f, 0.82f, 1f),
        TileType.Wall => new(1.15f, 1.10f, 1.02f, 1f),
        _ => Vector4.One,
    };

    public static Vector4 BorderColor(TileType type) => type switch
    {
        TileType.Water => new(0.04f, 0.12f, 0.18f, 0.40f),
        TileType.Wall => new(0.18f, 0.18f, 0.18f, 0.40f),
        TileType.Tree => new(0.05f, 0.13f, 0.06f, 0.40f),
        TileType.Bonfire => new(0.55f, 0.16f, 0.03f, 0.40f),
        _ => new(0.06f, 0.14f, 0.07f, 0.40f),
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
