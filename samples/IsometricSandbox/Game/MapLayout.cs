namespace IsometricSandbox.Game;

public static class MapLayout
{
    // G grass, T tree, W water, F bonfire, # wall
    public static readonly string[] Rows =
    [
        "####################",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGGGGGGTTTTGGGGGGG#",
        "#GGGGGGGTTTTGGGGGGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGGGGGGGFGGGGGGGGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGWWWWWWWWWWWWWWGG#",
        "#GGWWWWWWGGWWWWWWGG#",
        "#GGWWWWWWGGWWWWWWGG#",
        "#GGWWWWWWGGWWWWWWGG#",
        "#GGWWWWWWWWWWWWWWGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGGGTTTTGGGGGGGGGG#",
        "#GGGGTTTTGGGGGGGGGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "#GGGGGGGGGGGGGGGGGG#",
        "####################",
    ];

    public const int PlayerSpawnX = 1;
    public const int PlayerSpawnY = 1;
}
