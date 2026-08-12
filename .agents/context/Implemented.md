# Implemented Features

## ECS and Runtime

- Sparse ECS with generation-safe entities, dense/sparse component stores, serial queries, deferred mutation, scene ownership, and an explicit frame scheduler.
- Fixed-step game loop, deterministic movement/collision, camera following, and job-system dependency scheduling.

## Game and Sample

- Archer in the Forest sample with player movement/jump, critter wandering/fleeing, projectiles, scoring, restart, scene lifetime, and bounded simulation mode.
- Isometric and flat top-down rendering modes with stable entity depth ordering.

## Rendering and Shaders

- Vulkan renderer with swapchain lifecycle, batched textured sprites, descriptor-based texture sampling, parallel tile extraction, and secondary command recording.
- Incremental build-time GLSL-to-SPIR-V compilation and splash-screen texture loading.

## Platform and Input

- SDL3 windowing, keyboard/mouse input, fullscreen/resize handling, and cross-platform Vulkan surface creation.

## Jobs, Assets, and Diagnostics

- Dependency-aware work-stealing JobSystem with parallel-for support for approved workloads.
- PNG texture loading, procedural fallback assets, frame/simulation/allocation metrics, smoke tests, and Release benchmarks.

For milestone history and verification results, see `CompletedMilestones.md`.
# Milestone 3 — Sparse ECS Queries

- Added serial sparse queries for one, two, and three component intersections.
- Added smallest-store-driven iteration and by-reference struct callbacks.
- Added allocation-free query smoke coverage and isolated `SparseQuery_*` benchmarks.
# Milestone 4 — Explicit Frame Scheduler

- Added explicit stages, registrations, parallel groups, barriers, and plan diagnostics.
- Added JobSystem-backed parallel-group execution with access conflict validation.
- Preserved the existing scheduler and sample; migration remains Milestone 5 work.
