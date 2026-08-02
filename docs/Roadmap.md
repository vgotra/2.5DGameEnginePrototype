# Roadmap and Priorities

## Current MVP

- Windows window and input loop.
- Open tile map with continuous movement and collision.
- Arrow keys/WASD movement.
- Space jump two tiles in the last movement direction.
- Camera follow and isometric projection.
- Batched Vulkan shape renderer implementing `IRenderer`: render pass, per-swapchain framebuffers, SPIR-V shape shaders (compiled with glslc), graphics pipeline, per-frame staging uploads, acquire/present synchronization.
- White/black tile style: tiles and player drawn as white diamonds with black borders; the Vulkan NDC-Y orientation follows the y-down screen convention.
- Fullscreen switching: `F11` toggles borderless fullscreen; window drag-resize and fullscreen trigger a Vulkan swapchain rebuild (`VulkanRenderer.Resize`). `--fullscreen` starts fullscreen.
- `--2d` mode: flat top-down projection (white squares with black borders) via `ShapeKind` + a cartesian camera path.
- Vulkan is the only renderer; the window opens centered on the screen at 800x600.
- Map-bounds camera centering: the camera clamps to the map bounds so the map is centered on screen when it fits the viewport (fullscreen) and follows the player otherwise.
- Brief tests and resumable `.agents` context.

## Milestones

- **Milestone 1 — Win32 windowing + core gameplay.** SDK/build policy, core entity/clock types, isometric math, ECS storage, deterministic tile map, movement/collision/jump, camera follow, and Win32 window/input.
- **Milestone 2 — Vulkan + `IRenderer` + windowing.** Backend-neutral rendering contracts, the batched Vulkan renderer (`Engine.Rendering.Vulkan`), fullscreen switching with swapchain rebuild, `--2d` mode, Vulkan as the default backend, the centered 800x600 window, and map-bounds camera centering in fullscreen.
- **Milestone 3 — Cross-platform platform seams.** Backend-neutral `IGameWindow`/`IInputState` contracts, `PlatformKind` + `NativeWindowSurface`, the `Engine.Platform.Desktop.GamePlatform` host, and per-OS Vulkan loader/surface selection. The Linux/SDL2 and macOS backends are planned follow-ups (see `docs/LinuxSupportPlan.md`).

## Platform milestones

1. **Linux build + CI** — solution builds 0-warning on Linux; smoke tests run in CI.
2. **SDL2 window/input backend** (`Engine.Platform.Sdl2`) — `IGameWindow`/`IInputState` over SDL2 (X11 and Wayland), registered in `GamePlatform`.
3. **Vulkan surface on Linux** — `VK_KHR_xcb_surface`/`VK_KHR_wayland_surface` instance extension + surface creation in `VulkanRenderer`.
4. **Parity + verification** — white/black rendering matches Windows; sample verified on X11 and Wayland.
5. **macOS (later)** — same SDL2 backend via MoltenVK (`VK_MVK_macos_surface`).

## Next features, ordered by usefulness

1. **Stable game loop and window lifecycle** — **DONE (2026-08-03)**: frame pacing and clean shutdown (see [`FramePacingPlan.md`](FramePacingPlan.md)) — high-res `Stopwatch` dt + configurable frame cap (`--cap`); `VK_PRESENT_MODE_MAILBOX_KHR` (fallback FIFO) with triple buffering; a 3-slot frame-in-flight pool + per-swapchain-image fences replacing the single in-flight fence and the per-frame `vkQueueWaitIdle`; persistent dirty-gated vertex buffers; graceful ESC/window-close teardown with `ErrorOutOfDateKHR` auto-resize. Sim-to-present interpolation is a deferred optional follow-up.
2. **Texture sampling path** — sample textures in the fragment shader, bind descriptor sets per sprite batch, texture atlas support, and honor `SpritePacket.Texture`/`Material`.
3. **Asset loading** — PNG decoding, texture upload, sprite handles, and a small `assets/` convention.
4. **ECS queries and system scheduling** — replace sample-local state with ECS systems and explicit read/write access.
5. **Profiling and allocation metrics** — frame timings, draw calls, jobs, GC bytes, and Vulkan timestamps.
6. **Job dependencies and safe parallel work** — dependency-aware jobs for asset loading, large-map extraction, and uploads.
7. **Scene/save format** — explicit non-reflection serialization for tile maps, entities, and player state.
8. **Audio backend** — one-shot effects, music streaming, mixer buses, and listener/emitter support.
9. **Physics adapter** — integrate Jolt only when gameplay needs continuous collision, bodies, or raycasts.
10. **Animation and tile atlas support** — sprite animation, atlas metadata, and render batching.
11. **Debug tools** — collision overlays, entity inspector, frame graph, and input visualization.
12. **Minimal editor workflow** — only after runtime formats and asset loading are stable.
13. **Linux/SDL2 platform backend** — per `docs/LinuxSupportPlan.md` (X11/Wayland windowing and Vulkan surface; macOS via SDL2 + MoltenVK later).
14. **Replace `[DllImport]` with source-generated `[LibraryImport]`** — **DONE (2026-08-03)**: migrated the Win32 P/Invokes in `Engine.Platform.Win32` (`Win32Window.cs`, `Win32Input.cs`; 20 declarations + the blittable `POINT`/`RECT`/`MONITORINFO`/`MSG` structs) to `[LibraryImport]` on a new `internal static partial class NativeMethods` (separate `Win32Types.cs`). UTF-16 string APIs use `StringMarshalling.Utf16` with explicit `W` entry points (`CreateWindowExW`/`SetWindowTextW`/`GetMonitorInfoW`/`GetModuleHandleW`); `PeekMessage`/`DispatchMessage`/`CallWindowProc` have no literal export in `user32.dll` (only A/W via `WinUser.h` `#ifdef UNICODE`) and are pinned to the `A` variants to preserve the old Ansi-default binding; `TranslateMessage` is the lone literal-only exception. `GetWindowLongPtrW` now returns `nint` (matching native `LONG_PTR` and the setter). `SetLastError`/`Marshal.GetLastPInvokeError()` not added (unused). The `[UnmanagedFunctionPointer(Winapi)] WndProcDelegate` + `Marshal.GetFunctionPointerForDelegate` callback is preserved as-is. AOT/trim-safe and removes runtime marshalling stubs; `dotnet build` is 0 warnings (SDK `LibraryImportGenerator` is warning-clean under `TreatWarningsAsErrors`) and launch probes confirm default/`--fullscreen`/`--2d` modes all start cleanly and exit code 0 via `CloseMainWindow`. Optional verification plan (AOT publish, dotnet-counters/trace, BenchmarkDotNet, in-engine telemetry): [`LibraryImportVerificationPlan.md`](LibraryImportVerificationPlan.md). Originally documented as: "migrate the Win32 P/Invokes in `Engine.Platform.Win32` (`Win32Window.cs`, `Win32Input.cs`; 20 declarations) to `LibraryImportAttribute` on `partial` methods/classes. Preserve current behavior: UTF-16 string marshalling for the `user32.dll` string APIs (`CreateWindowEx`/`SetWindowText`/`GetModuleHandle` via `StringMarshalling.Utf16` — `LibraryImport` defaults to UTF-8), `SetLastError`/`Marshal.GetLastPInvokeError()` if used, and the `CallWindowProc` WndProc callback via function-pointer marshalling. Win32-only; AOT/trim-safe and removes runtime marshalling stubs."

## Deliberately deferred

Networking, skeletal animation, deferred rendering, consoles/mobile, a full editor, and production-scale content tooling are not MVP priorities.
