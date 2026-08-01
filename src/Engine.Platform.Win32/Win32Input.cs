using System.Runtime.InteropServices;

namespace Engine.Platform.Win32;

public sealed class Win32Input : IInputState
{
    private uint _current;
    private uint _previous;

    public void Update()
    {
        _previous = _current;
        _current = 0;
        Set(GameKey.Up, IsDown(0x57) || IsDown(0x26));
        Set(GameKey.Down, IsDown(0x53) || IsDown(0x28));
        Set(GameKey.Left, IsDown(0x41) || IsDown(0x25));
        Set(GameKey.Right, IsDown(0x44) || IsDown(0x27));
        Set(GameKey.Escape, IsDown(0x1B));
        Set(GameKey.Space, IsDown(0x20));
    }

    public bool IsDown(GameKey key) => (_current & Mask(key)) != 0;
    public bool WasPressed(GameKey key) => (_current & Mask(key)) != 0 && (_previous & Mask(key)) == 0;
    public bool WasReleased(GameKey key) => (_current & Mask(key)) == 0 && (_previous & Mask(key)) != 0;

    private void Set(GameKey key, bool down) { if (down) _current |= Mask(key); }
    private static uint Mask(GameKey key) => 1u << (int)key;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    private static bool IsDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
}
