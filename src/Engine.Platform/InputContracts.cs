using System.Numerics;

namespace Engine.Platform;

public enum GameKey { Up, Down, Left, Right, Escape, Space, Fullscreen, Restart }

public interface IInputState
{
    void Update();
    bool IsDown(GameKey key);
    bool WasPressed(GameKey key);
    bool WasReleased(GameKey key);
    Vector2 MousePosition { get; }
    bool IsMouseDown { get; }
    bool MousePressed { get; }
}
