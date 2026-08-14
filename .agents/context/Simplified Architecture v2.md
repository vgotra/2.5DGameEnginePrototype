# 2.5DGameEnginePrototype — Simplified Runtime Architecture

## Purpose

This document describes the implemented runtime architecture and constraints that future work must preserve. Future work is tracked in `PlannedMilestones.md`; verified history remains in `Implemented.md` and `CompletedMilestones.md`.

## Runtime hierarchy

```text
Application
    └── Game
          └── World
                └── Scene
                      ├── entities and ownership
                      ├── scene resources
                      ├── spawn/lifetime boundary
                      └── active environment
```

- `Application` and `GameHost` own platform, window, input, renderer, and game lifetime.
- `Game` owns the active world and game-level lifecycle.
- `World` owns the sparse ECS, scheduler, command application, persistent entities, and active scene.
- `Scene` owns scene membership and scene-lifetime entities; it is not a second ECS.
- Vulkan is the renderer and SDL3 is the desktop platform boundary.

## ECS and entity lifetime

`Engine.Ecs.Sparse` is canonical. Entities are generation-safe handles. Components live in independent dense/sparse `ComponentStore<T>` instances, and queries drive iteration from the smallest matching store.

The ECS is an engine and system implementation detail. Game-facing code should use gameplay spawn definitions and APIs in `Engine.App`, while engine systems may use typed sparse ECS operations directly.

Structural changes are deferred through `EntityCommands` and applied at explicit synchronization points. Scene-owned entities are removed when their scene unloads; world-owned entities survive scene changes; transient entities are short-lived gameplay objects.

## Frame execution and parallelism

Frame ordering is explicit. The scheduler owns stages, registration order, parallel groups, and barriers. The `JobSystem` owns worker execution, pooled work, waiting, and completion; it does not infer gameplay dependencies.

Multithreading is an engine capability, not a requirement that every system run in parallel:

- input, scene transitions, structural changes, and order-sensitive gameplay remain serial;
- independent workloads may use explicit parallel ranges or adaptive thresholds;
- asset and other blocking work may run in the background;
- parallel work must use disjoint data ranges and complete before dependent structural changes.

The supported public JobSystem contracts are `Run`, `ParallelFor`, `Wait`, and `IsComplete`. One job is never created per entity; work is batched into chunks.

## Rendering and extraction

Gameplay state is extracted into renderer-owned packets before Vulkan submission. The renderer does not iterate arbitrary gameplay state. Isometric and flat modes share the renderer boundary while using different projection and tile shapes.

Live renderer command recording is currently serial after CPU-side audit measurements showed no benefit from parallel recording for the representative workload. The audit path remains available for deterministic parity and future measurement.

## Public gameplay API

Immutable hero, monster, projectile, item, weapon, and skill definitions are available in `Engine.App`. Current gameplay spawning is deferred and returns reserved generation-safe entity handles; component population and scene registration are applied through `EntityCommands`.

The current game-facing API uses concise calls such as:

```csharp
var player = world.SpawnPlayer(playerDefinition);
var monster = scene.SpawnMonster(monsterDefinition);
```

Player, NPC, monster, projectile, item, and effect creation remains deferred through the shared command buffer. Callers do not create raw ECS entities or manually assemble gameplay components.

Gameplay features use immutable definitions and value-type state. Ability cooldowns feed weapon/projectile activation, projectile hits apply damage, and effects use fixed-step lifetime/VFX updates. High-frequency VFX use a fixed-capacity pool and are extracted into renderer-neutral items.

The renderer supports renderer-neutral scale/opacity metadata, eight-cell horizontal atlas animation frames, descriptor-backed material selection, and separate alpha/additive pipelines. Independent material shader parameters remain future work.

## Current constraints

- Sparse queries currently support one, two, or three component types; exclusion and exact-type filters are not implemented.
- Parallel queries are opt-in and must respect worker ownership and structural-change barriers.
- `World.Get<T>` is a fast API and requires a valid live entity.
- Component-store growth and structural changes may allocate; steady-state gameplay avoids them in hot loops.
- Asset decoding and fence-backed texture uploads are implemented in the asset and Vulkan layers. Indexed descriptor-array rendering is the preferred path with per-texture descriptor fallback. Texture eviction, atlas repacking, audio, physics, save serialization, and editor workflows remain separate future work.
- The 4096 outstanding-job limit and tiny-job queue churn are known JobSystem constraints.

## Development rules

- Keep the engine multithreaded but not parallel-by-default.
- Keep platform-specific code behind platform seams.
- Prefer explicit ownership, small APIs, and deterministic fixed-step behavior.
- Do not add scheduler inference, a second ECS, or speculative abstraction.
- Update `PlannedMilestones.md` when future work changes; do not append future milestones to this architecture reference.
