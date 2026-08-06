using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.Platform.Win32;

public sealed class Win32Input : IInputState
{
    private const int MouseLeftButton = 0x01;
    private const int KeyR = 0x52;

    private readonly nint _window;
    private uint _current;
    private uint _previous;
    private bool _mouseDown;
    private bool _mousePressed;
    private Vector2 _mousePosition;

    public Win32Input(nint window) => _window = window;

    public void Update()
    {
        _previous = _current;
        _current = 0;
        _mouseDown = QueryMouseState(out bool pressLatched);
        _mousePressed = pressLatched;
        _mousePosition = GetClientPosition();
        if (NativeMethods.GetForegroundWindow() != _window) return;
        Set(GameKey.Up, IsDown(0x57) || IsDown(0x26));
        Set(GameKey.Down, IsDown(0x53) || IsDown(0x28));
        Set(GameKey.Left, IsDown(0x41) || IsDown(0x25));
        Set(GameKey.Right, IsDown(0x44) || IsDown(0x27));
        Set(GameKey.Escape, IsDown(0x1B));
        Set(GameKey.Space, IsDown(0x20));
        Set(GameKey.Fullscreen, IsDown(0x7A));
        Set(GameKey.Restart, IsDown(KeyR));
    }

    public Vector2 MousePosition => _mousePosition;
    public bool IsMouseDown => _mouseDown;
    public bool MousePressed => _mousePressed;

    public bool IsDown(GameKey key) => (_current & Mask(key)) != 0;
    public bool WasPressed(GameKey key) => (_current & Mask(key)) != 0 && (_previous & Mask(key)) == 0;
    public bool WasReleased(GameKey key) => (_current & Mask(key)) == 0 && (_previous & Mask(key)) != 0;

    private Vector2 GetClientPosition()
    {
        if (!NativeMethods.GetCursorPos(out POINT point)) return _mousePosition;
        if (!NativeMethods.ScreenToClient(_window, ref point)) return _mousePosition;
        return new Vector2(point.X, point.Y);
    }

    private void Set(GameKey key, bool down) { if (down) _current |= Mask(key); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Mask(GameKey key) => 1u << (int)key;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDown(int key) => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool QueryMouseState(out bool pressLatched)
    {
        short state = NativeMethods.GetAsyncKeyState(MouseLeftButton);
        pressLatched = (state & 0x0001) != 0;
        return (state & 0x8000) != 0;
    }
}
