namespace Engine.App;

public sealed record SceneParams
{
    public MapId Map { get; init; }
    public SceneLighting Lighting { get; init; } = SceneLighting.Default;
    public CameraParams Camera { get; init; } = CameraParams.Default;
    public SceneRules Rules { get; init; }
    public string? BackgroundMusic { get; init; }
    public int Difficulty { get; init; }
}
