namespace Engine.Platform;

public enum GameKey { Up, Down, Left, Right, Escape, Space }

public interface IInputState
{
    bool IsDown(GameKey key);
    bool WasPressed(GameKey key);
    bool WasReleased(GameKey key);
}
