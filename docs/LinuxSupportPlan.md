# Linux Support Plan

Windows is the current primary supported platform, and the windowing/input layer is now **SDL3** (`ppy.SDL3-CS`), so Linux/macOS support is largely a verification exercise rather than new platform code. This document records the plan and the architectural seams in place.

## Status

- **Now:** Windows-first. Vulkan is the renderer on every platform; SDL3 provides windowing, input, and the Vulkan surface (`SDL_Vulkan_CreateSurface`). The native Win32 backend (`Engine.Platform.Win32`) has been removed.
- **Next:** Linux (X11 and Wayland) — SDL3 already targets them; the work is running and verifying on Linux (Vulkan loader name, SDL3 runtime selection), not writing new backends.
- **Later (possible):** macOS via SDL3 + MoltenVK.
- All gameplay-facing contracts are already platform-neutral (`Engine.Platform`, `Engine.Rendering`, `Engine.Audio`, `Engine.Physics`). Linux relies on the same Vulkan render path as Windows.

## Principles

1. Contracts never leak OS-specific types. `IGameWindow`, `IInputState`, `IRenderer` are the only boundaries gameplay code sees.
2. OS-specific code is confined to `Engine.Platform.*` backend projects.
3. The Vulkan backend consumes `NativeWindowSurface` (a `PlatformKind` + native window handle + an `IVulkanSurfaceFactory`). `IVulkanSurfaceFactory` supplies the required instance extensions (`SDL_Vulkan_GetInstanceExtensions`) and creates/destroys the surface (`SDL_Vulkan_CreateSurface`/`SDL_Vulkan_DestroySurface`) — the renderer never sees an HWND/XID, only `nint` handles.
4. Backends are selected at runtime through `Engine.Platform.Desktop.GamePlatform.CreateWindow(...)`; the sample never references a concrete platform directly.
