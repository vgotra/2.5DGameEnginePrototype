# Roadmap and Priorities

## Current MVP

- Windows window and input loop.
- Open tile map with continuous movement and collision.
- Arrow keys/WASD movement.
- Space jump two tiles in the last movement direction.
- Camera follow and isometric projection.
- Batched Vulkan shape renderer implementing `IRenderer` (`--vulkan`): render pass, per-swapchain framebuffers, SPIR-V shape shaders (compiled with glslc), graphics pipeline, per-frame staging uploads, acquire/present synchronization.
- GDI reference/fallback renderer (`--gdi`).
- Visual parity: tiles and player drawn as white diamonds with black borders in both backends; Vulkan NDC-Y orientation matches GDI.
- Fullscreen switching: `F11` toggles borderless fullscreen in both backends; window drag-resize and fullscreen trigger a Vulkan swapchain rebuild (`VulkanRenderer.Resize`). `--fullscreen` starts fullscreen.
- `--2d` mode: flat top-down projection (white squares with black borders) in both backends via `ShapeKind` + a cartesian camera path.
- Vulkan is the default backend (no backend flag runs `--vulkan`); the window opens centered on the screen at 800x600.
- Map-bounds camera centering: the camera clamps to the map bounds so the map is centered on screen when it fits the viewport (fullscreen) and follows the player otherwise.
- Smoke tests and resumable Codex context.

## Milestones

- **Milestone 1 — Win32 + GDI reference (Codex).** SDK/build policy, core entity/clock types, isometric math, ECS storage, deterministic tile map, movement/collision/jump, camera follow, Win32 window/input, and the initial GDI reference renderer. Authored with Codex.
- **Milestone 2 — Vulkan + `IRenderer` + windowing (OpenCode).** Backend-neutral rendering contracts, the batched Vulkan renderer (`Engine.Rendering.Vulkan`), fullscreen switching with swapchain rebuild, `--2d` mode, Vulkan as the default backend, the centered 800x600 window, and map-bounds camera centering in fullscreen. Authored with OpenCode.

## Next features, ordered by usefulness

1. **Stable game loop and window lifecycle** — frame pacing (vsync/frame limits) and clean shutdown. Resize/swapchain rebuild is now handled by both backends.
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

## Deliberately deferred

Networking, skeletal animation, deferred rendering, consoles/mobile, a full editor, and production-scale content tooling are not MVP priorities.
