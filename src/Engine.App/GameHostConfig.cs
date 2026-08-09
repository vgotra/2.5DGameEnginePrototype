using System.Numerics;

namespace Engine.App;

public readonly record struct GameHostConfig(
    string WindowTitle,
    Vector2 RenderResolution,
    double FrameCap,
    int SpriteCapacity,
    bool StartFullscreen,
    bool ShowMetrics,
    double SplashFramesPerSecond,
    double SplashMinimumSeconds);
