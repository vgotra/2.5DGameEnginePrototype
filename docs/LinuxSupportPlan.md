# Linux Support Plan

Windows is the current supported platform. This document records the plan and the architectural seams already in place so Linux (and later macOS) support can land without reworking the engine.

## Status

- **Now:** Windows-first. Vulkan is the renderer on every platform.
- **Next:** Linux (X11 and Wayland) — target backend is SDL2.
- **Later (possible):** macOS via SDL2 + MoltenVK.
- All gameplay-facing contracts are already platform-neutral (`Engine.Platform`, `Engine.Rendering`, `Engine.Audio`, `Engine.Physics`). Linux relies on the same Vulkan render path as Windows.

## Principles

1. Contracts never leak OS-specific types. `IGameWindow`, `IInputState`, `IRenderer` are the only boundaries gameplay code sees.
2. OS-specific code is confined to `Engine.Platform.*` backend projects.
3. The Vulkan backend consumes `NativeWindowSurface` (a `PlatformKind` + native handles) instead of raw HWND/HINSTANCE.
4. Backends are selected at runtime through `Engine.Platform.Desktop.GamePlatform.CreateWindow(...)`; the sample never references a concrete platform directly.

## What is already in place (this milestone)

- `Engine.Platform`: `PlatformKind` (`Win32`, `X11`, `Wayland`, `MacOs`) and `NativeWindowSurface` (`Kind`, `WindowHandle`, `DisplayHandle`, `ModuleHandle`).
- `IGameWindow` carries everything the renderer needs: `Size`, `ShouldClose`, `Fullscreen`, `SetFullscreen`, `SetTitle`, `Close`, `PumpEvents`, `NativeSurface`.
- `IInputState.Update()` added; input state is polled per frame through the interface.
- `Engine.Platform.Desktop` host project: `GamePlatform.CreateWindow` returns a `PlatformSession` (`IGameWindow` + `IInputState`). Win32 today; throws `PlatformNotSupportedException` elsewhere with a pointer to this doc.
- `VulkanRenderer(in NativeWindowSurface)`:
  - loader name per OS (`vulkan-1.dll` / `libvulkan.so.1`),
  - instance surface extension selected per `PlatformKind` (Win32 wired; X11/Wayland/macOS throw `PlatformNotSupportedException` until implemented).
- Sample now routes through the contracts/host; no Win32 types outside `Engine.Platform.Win32`.

## Remaining work (in order)

1. **Build & CI on Linux** — `dotnet build Engine.sln` must pass on Linux with the same 0-warning policy. The engine code already compiles cross-platform (P/Invoke is runtime-resolved); CI should run the smoke tests on Linux as well. The console smoke tests are headless and already portable.
2. **SDL2 window/input backend** (`Engine.Platform.Sdl2`, new project) —
   - Wrap `SDL_Init`, window creation, `SDL_PollEvent`, and the event pump via P/Invoke (add `SDL2` package/dll handling to `Directory.Packages.props` when chosen).
   - Implement `IGameWindow` (`Size`, `ShouldClose`, `Fullscreen`/`SetFullscreen`, `SetTitle`, `Close`, `PumpEvents`) and `IInputState` (`Update`, `IsDown`, `WasPressed`, `WasReleased`, keyboard mapping for `GameKey`).
   - Expose the native surface: `PlatformKind.X11` (`Display*` + `Window`) or `PlatformKind.Wayland` (`wl_display*` + `wl_surface*`) depending on the running display server.
   - Register in `GamePlatform.CreateWindow` behind `OperatingSystem.IsLinux()`.
   - SDL2 was chosen over native X11/Wayland P/Invoke because one code path covers X11, Wayland, and macOS, and it is MIT-licensed. Native X11/Wayland remains an alternative if SDL2 is later rejected.
3. **Vulkan surface on Linux** —
   - Enable `VK_KHR_xcb_surface` (or `VK_KHR_wayland_surface`) instance extension based on `NativeSurface.Kind`; implement the matching `vkCreateXcbSurfaceKHR` / `vkCreateWaylandSurfaceKHR` branch in `VulkanRenderer` (structure already exists).
   - Loader: `libvulkan.so.1` (already selected). Install the Vulkan SDK/loader + Mesa drivers on dev machines and CI.
4. **Parity and verification** —
   - Confirm white/black diamond and box rendering matches Windows (same SPIR-V, same math — only surface/swapchain differs).
   - Run the smoke tests in CI on Linux.
   - Manual probe: sample runs on both X11 and Wayland sessions.

## macOS (later)

- Same SDL2 backend (`PlatformKind.MacOs`); Vulkan via MoltenVK with `VK_MVK_macos_surface` (plus `VK_KHR_portability_*`). The extension-selection structure in `VulkanRenderer` already has the slot.

## Risks / known caveats

- Wayland differs from X11 at the surface level; SDL2 abstracts most of it, but fullscreen and swapchain behavior must be verified on each display server.
- Vulkan is the only renderer — no CPU fallback exists.
- Validation layers and SDK paths differ per distro; CI must pin the Vulkan SDK/loader versions.
- Present-mode selection and frame pacing (Mailbox/triple buffering, `--cap <fps>`) are implemented in `docs/FramePacingPlan.md`; swapchain present-mode negotiation must be re-verified on Linux, where driver support for `VK_PRESENT_MODE_MAILBOX_KHR` may differ.
