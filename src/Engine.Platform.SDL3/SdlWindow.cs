using System.Numerics;
using System.Runtime.InteropServices;
using SDL;
using Sdl = SDL.SDL3;

namespace Engine.Platform.SDL3;

internal sealed unsafe class SdlWindow : IGameWindow, IVulkanSurfaceFactory
{
    private readonly SDL_Window* _window;
    private bool _closed;
    private bool _disposed;
    private bool _mousePressedLatch;
    private bool _fullscreen;
    private bool _minimized;
    private bool _focused;
    private Vector2 _size;

    public SdlWindow(int width, int height, string title)
    {
        SdlRuntime.AddRef();
        try
        {
            SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_VULKAN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
            _window = Sdl.SDL_CreateWindow(title, width, height, flags);
            if (_window == null) throw new InvalidOperationException($"SDL_CreateWindow failed: {Sdl.SDL_GetError()}");
            int actualWidth, actualHeight;
            Sdl.SDL_GetWindowSize(_window, &actualWidth, &actualHeight);
            _size = new Vector2(actualWidth, actualHeight);
            UpdateWindowState();
        }
        catch
        {
            SdlRuntime.Release();
            throw;
        }
    }

    public Vector2 Size => _size;
    public bool ShouldClose => _closed;
    public bool Fullscreen => _fullscreen;
    public bool IsMinimized => _minimized;
    internal bool IsFocused => _focused;
    internal NativeWindowSurface NativeSurface => new(PlatformKind.Sdl3, (nint)_window, surfaceFactory: this);

    internal SDL_Window* Handle => _window;

    public void SetTitle(string title) => Sdl.SDL_SetWindowTitle(_window, title);

    public void SetFullscreen(bool fullscreen)
    {
        if (fullscreen == Fullscreen) return;
        if (!Sdl.SDL_SetWindowFullscreen(_window, fullscreen))
            throw new InvalidOperationException($"SDL_SetWindowFullscreen failed: {Sdl.SDL_GetError()}");
        UpdateWindowState();
    }

    public void Close() => _closed = true;

    public void PumpEvents()
    {
        Sdl.SDL_PumpEvents();
        SDL_Event evt;
        while (Sdl.SDL_PollEvent(&evt))
        {
            switch (evt.Type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                    _closed = true;
                    break;
                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    if (evt.window.data1 > 0 && evt.window.data2 > 0) _size = new Vector2(evt.window.data1, evt.window.data2);
                    break;
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    if (evt.button.Button == SDLButton.SDL_BUTTON_LEFT) _mousePressedLatch = true;
                    break;
            }
        }
        UpdateWindowState();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_window == null) return;
        Sdl.SDL_DestroyWindow(_window);
        SdlRuntime.Release();
    }

    internal bool ConsumeMousePressed()
    {
        bool value = _mousePressedLatch;
        _mousePressedLatch = false;
        return value;
    }

    private void UpdateWindowState()
    {
        SDL_WindowFlags flags = Sdl.SDL_GetWindowFlags(_window);
        _fullscreen = (flags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;
        _minimized = (flags & SDL_WindowFlags.SDL_WINDOW_MINIMIZED) != 0;
        _focused = (flags & SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) != 0;
    }

    public string[] RequiredInstanceExtensions
    {
        get
        {
            uint count = 0;
            byte** names = Sdl.SDL_Vulkan_GetInstanceExtensions(&count);
            if (names == null) throw new InvalidOperationException($"SDL_Vulkan_GetInstanceExtensions failed: {Sdl.SDL_GetError()}");
            string[] extensions = new string[count];
            for (uint i = 0; i < count; i++) extensions[i] = Marshal.PtrToStringUTF8((nint)names[i]) ?? string.Empty;
            return extensions;
        }
    }

    public nint CreateSurface(nint instanceHandle)
    {
        VkSurfaceKHR_T* surface = null;
        if (!Sdl.SDL_Vulkan_CreateSurface(_window, (VkInstance_T*)instanceHandle.ToPointer(), null, &surface))
            throw new InvalidOperationException($"SDL_Vulkan_CreateSurface failed: {Sdl.SDL_GetError()}");
        return (nint)surface;
    }

    public void DestroySurface(nint instanceHandle, nint surfaceHandle)
    {
        if (surfaceHandle == 0) return;
        Sdl.SDL_Vulkan_DestroySurface((VkInstance_T*)instanceHandle.ToPointer(), (VkSurfaceKHR_T*)surfaceHandle.ToPointer(), null);
    }
}
