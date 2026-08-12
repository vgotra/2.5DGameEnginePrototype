# Implemented Features

## Milestone 12 — Tune Multithreading

- Added explicit `Serial`, `Adaptive`, `Parallel`, and `Background` execution policies with stable system metadata.
- Added optional allocation/GC/timing diagnostics to the sparse frame scheduler.
- Classified sample input, AI, movement, collision, and projectile systems with fixed adaptive thresholds.
- Added policy benchmark cases and clean benchmark JobSystem ownership.

## Realistic ARPG Benchmark

- Added a deterministic workload with 1 player, 250 monsters, 100 projectiles, and 500 pooled effects.
- Added serial, adaptive-parallel, and forced-parallel gameplay benchmark cases with CPU sprite extraction.
- Added `--arpg` sample mode with camera-projected gameplay-sized rendering and bounded `--frames` runs.
- Added console coverage for population, determinism, mode parity, adaptive policy, extraction, and option parsing.

## Gameplay API

- Added reusable immutable hero, monster, weapon, skill, projectile, and item definitions in `Engine.App`.
- Added deferred `World.SpawnHero`, `SpawnMonster`, `SpawnProjectile`, and `SpawnItem` APIs with scene ownership and reserved entity handles.
- Added shared gameplay state components and smoke coverage for deferred activation and scene cleanup.
- Added the sample-owned `SampleEntitySpawner`, routing sample-specific PlayerState, Critter, and ArrowProjectile components through the runtime command buffer.

## Structural Command Buffer

- Replaced `WorldCommandBuffer` with FIFO `EntityCommands` supporting deferred Create, Destroy, Add, and Remove operations.
- Added reserved-entity lifecycle support with stale-operation safety and generation validation.
- Migrated sparse sample callers and console smoke coverage.

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

- Simplified work-stealing JobSystem with `Run`, `ParallelFor`, `Wait`, and `IsComplete`; arbitrary inter-job dependency scheduling removed.
- Deferred sparse structural mutations through `EntityCommands` with reserved entity creation and FIFO application.
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
