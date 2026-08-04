namespace Engine.Platform;

public enum GameKey { Up, Down, Left, Right, Escape, Space, Fullscreen }

public interface IInputState
{
    void Update();
    bool IsDown(GameKey key);
    bool WasPressed(GameKey key);
    bool WasReleased(GameKey key);
}
