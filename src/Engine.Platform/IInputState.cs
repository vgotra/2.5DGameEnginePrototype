using System.Numerics;

namespace Engine.Platform;

public interface IInputState
{
    void Update();
    bool IsDown(GameKey key);
    bool WasPressed(GameKey key);
    bool WasReleased(GameKey key);
    bool IsMouseButtonDown(MouseButton button);
    bool WasMouseButtonPressed(MouseButton button);
    Vector2 MousePosition { get; }
    bool IsMouseDown { get; }
    bool MousePressed { get; }
}
