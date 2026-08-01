# Roadmap and Priorities

## Current MVP

- Windows window and input loop.
- Open tile map with continuous movement and collision.
- Arrow keys/WASD movement.
- Space jump two tiles in the last movement direction.
- Camera follow and isometric projection.
- Double-buffered generated-shape renderer.
- Vulkan instance, surface, device, queue, swapchain, and clear-frame path.
- Smoke tests and resumable Codex context.

## Next features, ordered by usefulness

1. **Stable game loop and window lifecycle** — resize handling, close behavior, frame pacing, and clean shutdown.
2. **Real Vulkan sprite/shape renderer** — replace the GDI MVP path with shader modules, vertex buffers, batching, and `vkCmdDraw`.
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
