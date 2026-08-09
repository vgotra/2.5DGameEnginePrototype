# Roadmap and Priorities

## Completed

- **SDL3 platform backend** — SDL3 windowing/input + Vulkan surface on all OSes; native Win32 path removed.
- **Build-time shader compilation + splash screen** — incremental MSBuild `glslc` compile to committed `.spv`; startup splash with progress bar while textures load.
- **Isometric 2.5D render path** — iso diamond view + `--2d` flat mode, upright textured entities, stable depth sorting, camera follow.
- **Deterministic tile map** — 20×20 map (grass, river, forest, bonfire, wall border) with tile collision.
- **Continuous movement, collision, and jump** — player slides around blocked cells and jumps two tiles.
- **"Archer in the Forest" sample** — archer aims/shoots, 10 wandering/fleeing animals, score, no-respawn restart.
- **Texture sampling path (Initial)** — fragment-shader sampling + per-texture descriptor binds; `SpritePacket.Material`, atlases, sorting pending.
- **Asset loading (Initial)** — PNG decode + upload + `TextureHandle` + `assets/` convention; sync managed decode, sample-local `TextureLibrary`.
- **Profiling and allocation metrics (Initial)** — `--metrics` frame/sim/sprites/alloc/GC; draw calls, jobs, Vulkan timestamps pending.
- **Job dependencies and safe parallel work** — channels+work-stealing job scheduler with dependency graphs and parallel-for; async PNG decode, threshold-gated parallel map extraction, and parallel Vulkan secondary command recording.

## Planned

- **ECS queries and system scheduling** — ECS systems + explicit read/write access replacing sample-local state; ~100k entities with parallel multi-component queries.
- **Scene/save format** — non-reflection serialization for tile maps, entities, player state.
- **Audio backend** — one-shot effects, music streaming, mixer buses, listener/emitter.
- **Physics adapter** — Jolt for continuous collision, bodies, raycasts.
- **Animation and tile atlas support** — sprite animation, atlas metadata, render batching.
- **Debug tools** — collision overlays, entity inspector, frame graph, input visualization.
- **Minimal editor workflow** — after runtime formats and asset loading stabilize.
- **Virtual reality support** — OpenXR HMD (head tracking, per-eye rendering, VR input).
- **Mobile platforms** — Android/iOS via Vulkan + SDL3 + touch input.
- **Deferred** — networking, skeletal animation, deferred rendering, consoles, a full editor, production-scale content tooling.
