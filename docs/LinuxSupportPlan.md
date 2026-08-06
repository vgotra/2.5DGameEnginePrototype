# Linux Support Plan

Windows is the current supported platform. This document records the plan and the architectural seams already in place so Linux (and later macOS) support can land without reworking the engine.

## Status

- **Now:** Windows-first. Vulkan is the renderer on every platform.
- **Next:** Linux (X11 and Wayland) — target backend is **SDL3** (current stable; SDL2 is maintenance-only).
- **Later (possible):** macOS via SDL3 + MoltenVK, and unifying the native Win32 windowing/input path onto SDL3.
- All gameplay-facing contracts are already platform-neutral (`Engine.Platform`, `Engine.Rendering`, `Engine.Audio`, `Engine.Physics`). Linux relies on the same Vulkan render path as Windows.

## Principles

1. Contracts never leak OS-specific types. `IGameWindow`, `IInputState`, `IRenderer` are the only boundaries gameplay code sees.
2. OS-specific code is confined to `Engine.Platform.*` backend projects.
3. The Vulkan backend consumes `NativeWindowSurface` (a `PlatformKind` + native handles) instead of raw HWND/HINSTANCE.
4. Backends are selected at runtime through `Engine.Platform.Desktop.GamePlatform.CreateWindow(...)`; the sample never references a concrete platform directly.
