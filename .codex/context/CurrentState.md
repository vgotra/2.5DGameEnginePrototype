# Current State

## Status

Bootstrap and gameplay slice created: SDK/build policy, core entity and clock types, isometric math utility, initial worker scheduler, ECS storage, backend-neutral contracts, deterministic tile map, continuous player movement, collision resolution, camera following, Win32 window/input with WASD and arrow keys, Vortice Vulkan package integration, Win32 surface creation, physical-device selection, logical device, graphics queue, swapchain, and a full Vulkan draw path.

The Vulkan backend now implements the backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`). It owns a render pass, per-swapchain framebuffers, SPIR-V shape shaders (compiled with glslc, copied to `shaders/` in output), a graphics pipeline, descriptor pool allocator, texture uploader, batch renderer with per-frame staging uploads, and acquire/present synchronization. The sample's `--vulkan` mode runs the full isometric scene (tile map + jumping player) through `IRenderer`; the GDI `--gdi` path remains as a fallback. The Vulkan NDC-Y orientation defect was fixed by removing the Y negation in `shape.vert.glsl` (`gl_Position` now uses `ndc.y` directly), verified in the deployed SPIR-V (no `OpFNegate`). Both renderers now draw all tiles and the player as white diamonds with black borders (tiles: black border sprite + white fill sprite per cell; GDI: white brush + explicit black pen outline).

## Added this milestone (fullscreen switching + `--2d` mode)

Fullscreen: `Win32Window` now tracks its client size through `WM_SIZE` (single source for windowed drag-resize, `F11` toggling, and `--fullscreen` start) and exposes `SetFullscreen(bool)`, which switches the window style to `WS_POPUP` sized to the monitor bounds and restores the saved rect on exit (via `GetWindowRect`/`GetWindowLongPtr`/`MonitorFromWindow`/`GetMonitorInfo`/`SetWindowPos`). Both sample loops poll the window size each frame and call `camera.Resize` on change; the Vulkan loop additionally calls the new `VulkanRenderer.Resize`, which waits idle and recreates the swapchain, image views, and framebuffers. Command pool, command buffers, semaphores, and fence are now created once in `CreateCommandResources()` and survive resize. `GameKey.Fullscreen` (F11, VK=0x7A) was added to `Win32Input`.

`--2d` mode: `IsometricCamera` gained an `Isometric` flag; when false, `WorldToScreen` maps world units cartesian (64px per axis) instead of the isometric shear. `Engine.Rendering` gained `ShapeKind : byte { Diamond, Box }` and both `SpritePacket` and `ShapePacket` carry an optional `Shape` field (source-compatible; `SpritePacket` is now 48 bytes). The Vulkan `BatchRenderer` emits axis-aligned corner geometry for boxes; the GDI `Win32TileRenderer` draws `Rectangle` fills with the black pen. `RenderExtractionSystem` emits box pairs sized `(TileWidth, TileWidth)` in flat mode; the player is a 40x40 box in 2D, a 40x20 diamond in iso. The sample CLI now parses flags (`--vulkan`/`--gdi`, `--2d`, `--fullscreen`).

## Added this milestone (Vulkan default, 800x600 centered window, map centering)

No backend flag now defaults to Vulkan instead of GDI (`Program.cs`). The window opens centered on the primary screen at 800x600 (`Win32Window` centers via `GetSystemMetrics`; the Vulkan renderer's initial swapchain matches). `IsometricCamera.Follow` now clamps the camera to the map bounds in screen space: in iso the `(x+y)`/`(x-y)` axes are constrained so the 1280x640 map stays inside the viewport, and in cartesian mode `x`/`y` are constrained at 64px per axis. When the map fits a viewport axis (typical in fullscreen) that axis is centered on screen; otherwise the camera follows the player within the map. Because both backends share the camera, the map is centered identically in Vulkan and GDI fullscreen (previously it sat right of center, anchored to the player near the map's top-left).

Changed files: `src/Engine.Platform.Win32/Win32Window.cs`, `src/Engine.Platform.Win32/Win32Input.cs`, `src/Engine.Platform/InputContracts.cs`, `src/Engine.Rendering/RenderContracts.cs`, `src/Engine.Rendering.Vulkan/VulkanRenderer.cs`, `src/Engine.Rendering.Vulkan/BatchRenderer.cs`, `samples/IsometricSandbox/Program.cs`, `samples/IsometricSandbox/Game/CameraSystem.cs`, `samples/IsometricSandbox/Game/RenderExtractionSystem.cs`, `samples/IsometricSandbox/Game/Win32TileRenderer.cs`, `tests/Engine.Tests/Program.cs`.

Follow-up fix: the GDI fullscreen toggle left a stale frame until the player moved. The dirty-gated GDI loop repaints only on `renderDirty`; the fullscreen resize invalidates the client area, and the default window procedure's paint pass wiped the freshly blitted frame. `Win32Window` now suppresses `WM_ERASEBKGND` (returns 1, no background erase) and flags repaint on `WM_PAINT` via `ConsumeRepaint()`, which the GDI loop folds into `renderDirty`; `Win32TileRenderer.Draw` calls `ValidateRect` after the blit so a pending paint can't erase it. This also covers windowed drag-resize and cover/uncover. Vulkan was unaffected (it repaints every frame).

## Verification

`dotnet build Engine.sln --nologo` passes with 0 warnings and 0 errors. `dotnet run --project tests/Engine.Tests` prints `Smoke tests passed` (assertions cover iso and flat camera centering in an 800x600 viewport plus a 1920x1080 "fullscreen" viewport, flat-mode extraction count, box shape, and cartesian camera mapping). Launch probes confirmed the default (no flags) starts Vulkan, and `--vulkan`, `--gdi`, `--2d`, `--fullscreen` combos all start and run the loop with the 800x600 centered window. Deployed `shape.vert.spv` hash matches the asset, and `spirv-dis` confirms the position store uses `ndc.y` directly.

## Next three actions

1. Stable game loop: frame pacing (vsync/frame limits) and clean shutdown; resize/swapchain rebuild is now handled.
2. Texture sampling path: bind descriptor sets, texture-blended sprites, and texture atlas support.
3. Profiling counters and allocation measurements for the draw path.

## Risks

The MVP renderer does one staging-buffer upload + `vkQueueWaitIdle` per frame, which serializes the queue; this is correct but not performance-optimal. The batch renderer draws shapes only; `SpritePacket` texture/material handles are currently ignored.
