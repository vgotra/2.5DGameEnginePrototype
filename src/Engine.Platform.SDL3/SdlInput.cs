using System.Numerics;
using System.Runtime.CompilerServices;
using SDL;
using Sdl = SDL.SDL3;

namespace Engine.Platform.SDL3;

public sealed unsafe class SdlInput(SdlWindow window) : IInputState
{
    private readonly SDLBool* _keyboardState = Sdl.SDL_GetKeyboardState(null);
    private uint _current;
    private uint _previous;
    private bool _mouseDown;
    private bool _mousePressed;
    private Vector2 _mousePosition;

    public void Update()
    {
        _previous = _current;
        _current = 0;
        float x, y;
        SDL_MouseButtonFlags mouseFlags = Sdl.SDL_GetMouseState(&x, &y);
        _mouseDown = (mouseFlags & SDL_MouseButtonFlags.SDL_BUTTON_LMASK) != 0;
        _mousePressed = window.ConsumeMousePressed();
        _mousePosition = new Vector2(x, y);
        if ((Sdl.SDL_GetWindowFlags(window.Handle) & SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) == 0) return;
        Set(GameKey.Up, IsDown(SDL_Scancode.SDL_SCANCODE_W) || IsDown(SDL_Scancode.SDL_SCANCODE_UP));
        Set(GameKey.Down, IsDown(SDL_Scancode.SDL_SCANCODE_S) || IsDown(SDL_Scancode.SDL_SCANCODE_DOWN));
        Set(GameKey.Left, IsDown(SDL_Scancode.SDL_SCANCODE_A) || IsDown(SDL_Scancode.SDL_SCANCODE_LEFT));
        Set(GameKey.Right, IsDown(SDL_Scancode.SDL_SCANCODE_D) || IsDown(SDL_Scancode.SDL_SCANCODE_RIGHT));
        Set(GameKey.Escape, IsDown(SDL_Scancode.SDL_SCANCODE_ESCAPE));
        Set(GameKey.Space, IsDown(SDL_Scancode.SDL_SCANCODE_SPACE));
        Set(GameKey.Fullscreen, IsDown(SDL_Scancode.SDL_SCANCODE_F11));
        Set(GameKey.Restart, IsDown(SDL_Scancode.SDL_SCANCODE_R));
    }

    public Vector2 MousePosition => _mousePosition;
    public bool IsMouseDown => _mouseDown;
    public bool MousePressed => _mousePressed;

    public bool IsDown(GameKey key) => (_current & Mask(key)) != 0;
    public bool WasPressed(GameKey key) => (_current & Mask(key)) != 0 && (_previous & Mask(key)) == 0;
    public bool WasReleased(GameKey key) => (_current & Mask(key)) == 0 && (_previous & Mask(key)) != 0;

    private void Set(GameKey key, bool down) { if (down) _current |= Mask(key); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Mask(GameKey key) => 1u << (int)key;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDown(SDL_Scancode scancode) => _keyboardState[(int)scancode];
}
