# Next Steps

Milestone completed: backend-neutral rendering contracts, ECS storage, the Vulkan `IRenderer` draw path for the full scene, fullscreen switching with swapchain rebuild, a flat `--2d` projection mode, Vulkan as the default backend, the centered 800x600 window, and map-bounds camera centering in fullscreen. The next actions below are in order.

## Current milestone follow-ups

1. Stable game loop and window lifecycle: frame pacing and clean shutdown — see `docs/FramePacingPlan.md` for the diagnosis and full approach. Root cause: Vulkan delivers 60 Hz fixed-step motion through a FIFO-vsync, double-buffered, single-fence pipeline with a per-frame staging upload + `vkQueueWaitIdle` before present, so timing jitter shows as judder; GDI's immediate `BitBlt` (no vsync/waits) does not. Implement: high-res dt + frame cap/vsync policy; prefer `VK_PRESENT_MODE_MAILBOX_KHR` (fallback FIFO) with triple buffering; per-swapchain-image fences dropping the per-frame `vkQueueWaitIdle`; persistent dirty-gated vertex buffers; graceful ESC/window-close teardown. Resize/swapchain rebuild and fullscreen switching are already handled.
2. Texture path: sample textures in the fragment shader, bind descriptor sets per sprite batch, add texture atlas support, and honor `SpritePacket.Texture`/`Material`.
3. Profiling: frame timings, draw calls, staging-buffer allocations, and Vulkan timestamps.
4. Linux platform backend: SDL2 window/input (`Engine.Platform.Sdl2`), X11/Wayland Vulkan surface, CI on Linux (see `docs/LinuxSupportPlan.md`). Cross-platform seams (contracts, `GamePlatform` host, `NativeWindowSurface`, per-OS loader/surface selection) are already in place.

## Roadmap status

Completed: SDK/build policy, core entity/clock types, isometric math, ECS storage, worker-scheduler foundation, backend-neutral contracts (rendering/audio/physics/platform), deterministic tile map, movement + collision + jump + camera, Win32 window/input, Vulkan setup + full batched draw path via `IRenderer`, GDI reference renderer, smoke tests, visual parity between backends (white/black styling, matching orientation), fullscreen switching (`F11`/`--fullscreen`) with Vulkan swapchain rebuild, `--2d` flat mode in both backends, Vulkan as the default backend, the centered 800x600 window, map-bounds camera centering in fullscreen, and cross-platform platform seams (`IGameWindow`/`IInputState`/`PlatformKind`/`NativeWindowSurface`, `Engine.Platform.Desktop.GamePlatform` host, per-OS Vulkan loader/surface selection).

Pending (see `docs/Roadmap.md`): frame pacing/clean shutdown, texture sampling, asset loading, ECS queries/system scheduling, profiling, job dependencies, scene/save format, audio backend, physics adapter, animation/tile atlas, debug tools, editor, Linux (SDL2) platform backend.
