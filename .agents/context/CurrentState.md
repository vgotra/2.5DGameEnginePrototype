# Current State

## Status

The engine is a Windows-first .NET 10 prototype for 2D/2.5D isometric games. The MVP vertical slice is complete: SDK/build policy, core entity and clock types, isometric math, ECS storage, backend-neutral contracts (rendering/audio/physics/platform), a deterministic tile map, continuous movement with collision and jump, camera following, Win32 window/input, and both render paths.

Rendering is split between a backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`) and two backends that draw identical scenes — white diamonds with black borders in iso, white boxes with black borders in `--2d`:

- **Vulkan (`Engine.Rendering.Vulkan`, default)** — swapchain, render pass, per-swapchain framebuffers, SPIR-V shape shaders (GLSL compiled with glslc, copied to `shaders/` in output), graphics pipeline, descriptor pool allocator, texture uploader, and a batch renderer that accumulates submitted packets and issues one indexed draw per frame via a staging-buffer upload. `SpritePacket.Texture`/`Material` handles exist but are not yet sampled. Screen space is y-down and the vertex shader stores `gl_Position` with `ndc.y` unchanged (Vulkan NDC y = -1 maps to the top); no Y negation is applied.
- **GDI (`--gdi`, fallback)** — cached double-buffered reference renderer; fills diamonds/boxes with a white brush and outlines with a 1px black pen.

Windowing (`Engine.Platform.Win32`): the window opens centered on the primary screen at 800x600. `Win32Window` reports client size through `WM_SIZE` (single source for windowed drag-resize, `F11` toggling, and `--fullscreen` start) and `SetFullscreen(bool)` switches to borderless `WS_POPUP` sized to the monitor bounds, restoring the saved rect on exit. Both loops poll the size each frame and call `camera.Resize`; the Vulkan loop also calls `VulkanRenderer.Resize`, which recreates the swapchain, image views, and framebuffers (command pool, command buffers, semaphores, and fence are created once and survive resize). `Win32Window` suppresses `WM_ERASEBKGND` and flags repaint via `ConsumeRepaint()`, and `Win32TileRenderer.Draw` calls `ValidateRect` after each blit so dirty-gated GDI rendering survives resizes/fullscreen without a stale or wiped frame.

No backend flag defaults to Vulkan. `--2d` is a projection/geometry modifier, not a backend: the camera switches to a cartesian mapping (64px per axis) and both backends emit axis-aligned boxes via `ShapeKind.Box` (the player is a 40x40 box in 2D, a 40x20 diamond in iso). `IsometricCamera.Follow` clamps the camera to the map bounds in screen space (iso constrains the `(x+y)`/`(x-y)` axes so the 1280x640 map stays in the viewport; cartesian constrains `x`/`y`), so when the map fits a viewport axis that axis is centered on screen and otherwise the camera follows the player; both backends share the camera, so the map is centered identically in Vulkan and GDI fullscreen.

The platform layer is cross-platform-ready (Windows only today): `Engine.Platform` defines `IGameWindow` (size, close, fullscreen, repaint, native surface), `IInputState` (polled per frame), `PlatformKind`, and `NativeWindowSurface`; `Engine.Platform.Desktop.GamePlatform.CreateWindow` is the factory that returns a `PlatformSession` and selects the Win32 backend now (throwing `PlatformNotSupportedException` elsewhere). `VulkanRenderer` consumes `NativeWindowSurface` and selects the Vulkan loader name per OS; Win32 surface is wired, X11/Wayland/macOS slots throw until the SDL2 backend lands. The sample talks only to contracts + `Engine.Platform.Desktop`. See `docs/LinuxSupportPlan.md`.

## Verification

`dotnet build Engine.sln --nologo` passes with 0 warnings and 0 errors. `dotnet run --project tests/Engine.Tests` prints `Smoke tests passed` (assertions cover iso and flat camera centering in an 800x600 viewport plus a 1920x1080 "fullscreen" viewport, flat-mode extraction count, box shape, and cartesian camera mapping). Launch probes confirmed default (no flags) starts Vulkan, and `--vulkan`, `--gdi`, `--2d`, `--fullscreen` combos all start and run the loop with the 800x600 centered window.

## Next three actions

1. Stable game loop: frame pacing (vsync/frame limits) and clean shutdown; resize/swapchain rebuild is now handled.
2. Texture sampling path: bind descriptor sets, texture-blended sprites, and texture atlas support.
3. Profiling counters and allocation measurements for the draw path.
4. Linux platform backend (SDL2 window/input + X11/Wayland Vulkan surface) per `docs/LinuxSupportPlan.md`.

## Risks

The MVP renderer does one staging-buffer upload + `vkQueueWaitIdle` per frame, which serializes the queue; this is correct but not performance-optimal. The batch renderer draws shapes only; `SpritePacket` texture/material handles are currently ignored.
