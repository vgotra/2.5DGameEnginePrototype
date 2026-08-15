# Implemented Features

## Delete and Document

- Documentation now describes the shipped sparse-set ECS, explicit frame scheduler, adaptive/targeted multithreading, and serial live renderer command recording.
- Obsolete archetype, automatic dependency-inference, mandatory-parallel, and migration-plan instructions were removed from current architecture guidance.

## JobSystem Capacity Measurement

- Verified exact 4096-slot saturation, overflow rejection, and slot reuse after completion.
- Added allocation-free representative benchmarks for small `Run` batches, tiny `ParallelFor`, and slot reuse.
- Measured larger queued `Run` bursts as channel-segment allocation churn without changing JobSystem internals.

## Presentation Policy Measurement

- Exposed requested/selected Vulkan present mode, fallback state, and swapchain image count through read-only diagnostics.
- Added allocation-free FPS and frame-time statistics with average, median, p95, p99, and maximum values.
- Added benchmark coverage for uncapped-like, 60/120/144/240 FPS, and jittered frame intervals.
- Preserved MAILBOX preference and FIFO fallback without adaptive presentation changes.

## Tune Multithreading

- Added explicit `Serial`, `Adaptive`, `Parallel`, and `Background` execution policies with stable system metadata.
- Added optional allocation/GC/timing diagnostics to the sparse frame scheduler.
- Classified sample input, AI, movement, collision, and projectile systems with fixed adaptive thresholds.
- Added focused benchmark ownership and zero-allocation Release verification.

## Rendering Audit

- Added renderer-owned serial/parallel command-preparation audit helpers with deterministic chunking and checksums.
- Added representative renderer audit workloads and smoke parity coverage; the active benchmark gate is now a focused 44-case catalog.
- Simplified live renderer command recording to one serial secondary command buffer after the audit measured parallel preparation overhead above serial work; the audit path reports zero steady-state benchmark allocations after warm-up.

## Realistic ARPG Benchmark

- Added a deterministic workload with 1 player, 250 monsters, 100 projectiles, and 500 pooled effects.
- Added serial, adaptive-parallel, and forced-parallel gameplay benchmark cases with CPU sprite extraction.
- Added the canonical ARPG sample with camera-projected gameplay-sized rendering and bounded `--frames` runs.
- Added the separate IsometricSimulation executable for deterministic serial/parallel workload validation.
- Added console coverage for population, determinism, execution parity, adaptive policy, extraction, and bounded options.

## Gameplay API

- Added reusable immutable hero, monster, weapon, skill, projectile, and item definitions in `Engine.App`.
- Added deferred `World.SpawnHero`, `SpawnMonster`, `SpawnProjectile`, and `SpawnItem` APIs with scene ownership and reserved entity handles.
- Added shared gameplay state components and smoke coverage for deferred activation and scene cleanup.
- Added the sample-owned `SampleEntitySpawner`, routing sample-specific PlayerState, Critter, and ArrowProjectile components through the runtime command buffer.
- Added player ability cooldowns, weapon/projectile activation, NPC definitions and behavior state, projectile damage, and impact/muzzle VFX flow.
- Added fixed-capacity pooled VFX extraction with fixed-step updates and deferred lifetime handling.

## Gameplay Runtime and Content

- Added typed hero, enemy, NPC, skill, item, effect, quest, scene, map, VFX, sound, and logical model identifiers with immutable definition lookup.
- Added `GameContent`, `GameplayContracts`, and value-type runtime state for attributes, derived stats, effects, quests, AI intents, companions, navigation, combat reactions, and dialogue/capability composition.
- Added the Village → Goblin Forest gameplay scenario with quest activation, directed travel unlock, goblin spawning, Cleric companion support, deterministic combat progress, loot/equipment reward, and return completion.

## Input, Movement, and Navigation

- Added device-neutral keyboard, mouse, gamepad, and virtual action bindings with fixed-step `PlayerCommand` and `CharacterIntent` consumption.
- Added ten-slot hotbar/action-set mapping, modifier-based gamepad skill slots, deterministic movement commands, collision-aware grid navigation, cached routes, and renderer-neutral navigation reactions.

## glTF Character Asset Pipeline

- Added build-time glTF/GLB decoding for the supported mesh, material, texture, node, skin, joint, weight, and skeletal-animation subset.
- Added deterministic texture sampling, skeletal pose evaluation, weighted skinning, software sprite raster baking, atlas frame metadata, and logical cooked-character asset registration.
- Runtime loads cooked atlas data once through the texture library and retains PNG/procedural fallback assets when generated glTF output is absent. Runtime gameplay never reads source glTF or bake manifests.

## Structural Command Buffer

- Replaced `WorldCommandBuffer` with FIFO `EntityCommands` supporting deferred Create, Destroy, Add, and Remove operations.
- Added reserved-entity lifecycle support with stale-operation safety and generation validation.
- Migrated sparse sample callers and console smoke coverage.

## ECS and Runtime

- Sparse ECS with generation-safe entities, dense/sparse component stores, serial queries, deferred mutation, scene ownership, and an explicit frame scheduler.
- Fixed-step game loop, deterministic movement/collision, camera following, and explicit frame scheduling with caller-owned JobSystem barriers.

## Game and Sample

- Archer in the Forest sample with player movement/jump, critter wandering/fleeing, projectiles, scoring, restart, scene lifetime, and bounded simulation mode.
- Canonical 2.5D isometric rendering with diamond tile extraction and stable entity depth ordering.

## Rendering and Shaders

- Vulkan renderer with swapchain lifecycle, batched textured sprites, descriptor-based texture sampling, parallel tile extraction, and secondary command recording.
- Sprite extraction and Vulkan geometry support atlas animation frames, scale/opacity metadata, material-selected descriptors, and dedicated additive blending.
- Render-thread-owned Vulkan texture upload batches use bounded tickets, binary-fence polling, retained staging resources, completion-only texture publication, and allocation-free diagnostics.
- Optional descriptor indexing uses a stable 1024-entry sampled-image array, indexed shader variant, per-instance texture indices, and explicit Auto/Fallback/Indexed startup selection.
- Incremental build-time GLSL-to-SPIR-V compilation and splash-screen texture loading.

## Platform and Input

- SDL3 windowing, keyboard/mouse input, fullscreen/resize handling, and cross-platform Vulkan surface creation.

## Jobs, Assets, and Diagnostics

- Simplified work-stealing JobSystem with `Run`, `ParallelFor`, `Wait`, and `IsComplete`; arbitrary inter-job dependency scheduling removed.
- Deferred sparse structural mutations through `EntityCommands` with reserved entity creation and FIFO application.
- Asynchronous PNG decoding with unmanaged decoded buffers, procedural fallback assets, upload/descriptor diagnostics, frame/simulation/allocation metrics, smoke tests, and Release benchmarks.
- The plain console smoke suite retains contract and regression coverage; the Release benchmark gate uses a focused representative catalog with zero-allocation enforcement.

For milestone history and verification results, see `CompletedMilestones.md`.
# Milestone 3 — Sparse ECS Queries

- Added serial sparse queries for one, two, and three component intersections.
- Added smallest-store-driven iteration and by-reference struct callbacks.
- Added allocation-free query smoke coverage and isolated `SparseQuery_*` benchmarks.
# Milestone 4 — Explicit Frame Scheduler

- Added explicit stages, registrations, parallel groups, barriers, and plan diagnostics.
- Added JobSystem-backed parallel-group execution with access conflict validation.
- Preserved the existing scheduler and sample; migration remains Milestone 5 work.
