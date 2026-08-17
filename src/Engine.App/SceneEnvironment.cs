using System.Numerics;

namespace Engine.App;

public readonly record struct SceneLighting(Vector4 AmbientColor, float AmbientIntensity)
{
    public static SceneLighting Default => new(Vector4.One, 1f);
}

public readonly record struct CameraParams(Vector2 Position, float Zoom)
{
    public static CameraParams Default => new(Vector2.Zero, 1f);
}

public readonly record struct SceneRules(int Difficulty = 0, bool AllowRespawn = true);
