using System.Numerics;

namespace Engine.Platform;

public interface IGameWindow : IDisposable
{
    Vector2 Size { get; }
    bool ShouldClose { get; }
    void PumpEvents();
}
