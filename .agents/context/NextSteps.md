# Next Steps

Milestone completed: backend-neutral rendering contracts, ECS storage, the Vulkan `IRenderer` draw path for the full scene, fullscreen switching with swapchain rebuild, a flat `--2d` projection mode, Vulkan as the default backend, the centered 800x600 window, map-bounds camera centering in fullscreen, and frame pacing / clean shutdown (roadmap item 1). The next actions below are in order.

## Current milestone follow-ups

1. Texture path: sample textures in the fragment shader, bind descriptor sets per sprite batch, add texture atlas support, and honor `SpritePacket.Texture`/`Material`.
2. Asset loading: PNG decoding, texture upload, sprite handles, and a small `assets/` convention.
3. ECS queries + system scheduling: replace sample-local state with ECS systems and explicit read/write access.
4. Profiling: frame timings, draw calls, staging-buffer allocations, and Vulkan timestamps.
5. Linux platform backend: SDL2 window/input (`Engine.Platform.Sdl2`), X11/Wayland Vulkan surface, CI on Linux (see `docs/LinuxSupportPlan.md`). Cross-platform seams (contracts, `GamePlatform` host, `NativeWindowSurface`, per-OS loader/surface selection) are already in place.

## Roadmap status

Completed: SDK/build policy, core entity/clock types, isometric math, ECS storage, worker-scheduler foundation, backend-neutral contracts (rendering/audio/physics/platform), deterministic tile map, movement + collision + jump + camera, Win32 window/input, Vulkan setup + full batched draw path via `IRenderer`, smoke tests, white/black tile styling (diamonds in iso, boxes in `--2d`), fullscreen switching (`F11`/`--fullscreen`) with Vulkan swapchain rebuild, `--2d` flat mode, the centered 800x600 window, map-bounds camera centering in fullscreen, cross-platform platform seams (`IGameWindow`/`IInputState`/`PlatformKind`/`NativeWindowSurface`, `Engine.Platform.Desktop.GamePlatform` host, per-OS Vulkan loader/surface selection), removal of the GDI reference renderer (Vulkan is the only renderer), and frame pacing + clean shutdown (Mailbox/triple buffering, 3-slot frame-in-flight pool + per-swapchain-image fences, no per-frame `vkQueueWaitIdle`, FNV dirty-gated persistent buffers, `Engine.Core.FrameTimer` with `--cap`, `ErrorOutOfDateKHR` auto-resize) — see `docs/FramePacingPlan.md`.

Pending (see `docs/Roadmap.md`): texture sampling, asset loading, ECS queries/system scheduling, profiling, job dependencies, scene/save format, audio backend, physics adapter, animation/tile atlas, debug tools, editor, Linux (SDL2) platform backend, P/Invoke → `LibraryImport` migration (Win32).
