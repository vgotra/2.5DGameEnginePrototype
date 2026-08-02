namespace Engine.Platform;

public enum GameKey { Up, Down, Left, Right, Escape, Space, Fullscreen }

public interface IInputState
{
    /// <summary>Samples the platform input devices; call once per frame before querying state.</summary>
    void Update();
    bool IsDown(GameKey key);
    bool WasPressed(GameKey key);
    bool WasReleased(GameKey key);
}
