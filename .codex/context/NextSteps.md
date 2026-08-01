# Next Steps

Milestone completed: backend-neutral rendering contracts, ECS storage, and the Vulkan `IRenderer` draw path for the full scene. The next actions below are in order.

## Current milestone follow-ups

1. Stable game loop and window lifecycle: resize triggers swapchain rebuild, frame pacing, clean shutdown.
2. Texture path: sample textures in the fragment shader, bind descriptor sets per sprite batch, add texture atlas support, and honor `SpritePacket.Texture`/`Material`.
3. Profiling: frame timings, draw calls, staging-buffer allocations, and Vulkan timestamps.

## Roadmap status

Completed: SDK/build policy, core entity/clock types, isometric math, ECS storage, worker-scheduler foundation, backend-neutral contracts (rendering/audio/physics/platform), deterministic tile map, movement + collision + jump + camera, Win32 window/input, Vulkan setup + full batched draw path via `IRenderer`, GDI reference renderer, smoke tests, and visual parity between backends (white/black styling, matching orientation).

Pending (see `docs/Roadmap.md`): stable loop/resize, texture sampling, asset loading, ECS queries/system scheduling, profiling, job dependencies, scene/save format, audio backend, physics adapter, animation/tile atlas, debug tools, editor.
