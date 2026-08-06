using SDL;
using Sdl = SDL.SDL3;

namespace Engine.Platform.SDL3;

internal static class SdlRuntime
{
    private static int _refCount;

    public static void AddRef()
    {
        if (Interlocked.Increment(ref _refCount) != 1) return;
        if (!Sdl.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_EVENTS))
            throw new InvalidOperationException($"SDL_Init failed: {Sdl.SDL_GetError()}");
    }

    public static void Release()
    {
        if (Interlocked.Decrement(ref _refCount) != 0) return;
        Sdl.SDL_Quit();
    }
}
