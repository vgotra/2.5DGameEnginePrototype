# Implemented

Shipped milestones. Next actions live in `Roadmap.md`.

- **Milestone 0 baseline runtime measurement** — added a dedicated Release benchmark matrix for current archetype ECS serial/parallel updates across 100–100,000 entities, including timing, allocations, GC, scheduler overhead, jobs/frame, worker participation, and utilization.

- **Game / World / Scene runtime contracts** — added explicit game/world/scene ownership and lifetime boundaries while preserving the existing archetype ECS, scheduler, JobSystem, and renderer; scene-owned entities are cleaned up on scene unload.

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
- **ECS queries and system scheduling** — archetype `World` with generation-safe handles, cached queries (1/2/3 components) with serial `ForEach` and parallel `ForEachParallel` dispatch, a `SystemScheduler` with read/write access-based conflict ordering, and a `WorldCommandBuffer` for deferred mutation; the sample runs on four ECS systems, and `--simulation` stresses 100k critters through parallel two-component queries.
