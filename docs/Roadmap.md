# Roadmap and Priorities

## Next features, ordered by usefulness

1. **Texture sampling path** — sample textures in the fragment shader, bind descriptor sets per sprite batch, texture atlas support, and honor `SpritePacket.Texture`/`Material`.
2. **Asset loading** — PNG decoding, texture upload, sprite handles, and a small `assets/` convention.
3. **ECS queries and system scheduling** — replace sample-local state with ECS systems and explicit read/write access. Target scale: ~100k entities with parallel multi-component queries.
4. **Profiling and allocation metrics** — frame timings, draw calls, jobs, GC bytes, and Vulkan timestamps.
5. **Job dependencies and safe parallel work** — dependency-aware jobs for asset loading, large-map extraction, and uploads.
6. **Scene/save format** — explicit non-reflection serialization for tile maps, entities, and player state.
7. **Audio backend** — one-shot effects, music streaming, mixer buses, and listener/emitter support.
8. **Physics adapter** — integrate Jolt only when gameplay needs continuous collision, bodies, or raycasts.
9. **Animation and tile atlas support** — sprite animation, atlas metadata, and render batching.
10. **Debug tools** — collision overlays, entity inspector, frame graph, and input visualization.
11. **Minimal editor workflow** — only after runtime formats and asset loading are stable.
12. **Linux/SDL2 platform backend** — per `docs/LinuxSupportPlan.md` (X11/Wayland windowing and Vulkan surface; macOS via SDL2 + MoltenVK later).

## Deliberately deferred

Networking, skeletal animation, deferred rendering, consoles/mobile, a full editor, and production-scale content tooling are not MVP priorities.
