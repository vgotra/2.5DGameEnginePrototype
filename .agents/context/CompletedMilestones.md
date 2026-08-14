# Completed Milestones

Historical benchmark matrices in this file describe the suite at the time of each milestone. They are not the current default gate; the current catalog is the focused 35-case suite documented in `ProjectConfig.md`.

## 2026-08-13 — Milestone 14: Delete and Document

Verified:
- current architecture guidance now matches the shipped sparse-set ECS and explicit scheduler;
- obsolete archetype migration, automatic dependency-graph, and mandatory-parallel instructions were removed;
- README, AGENTS.md, Roadmap.md, Implemented.md, and architecture guidance were synchronized;
- historical milestone records were preserved where needed for accurate history.

## 2026-08-13 — Milestone 13: Rendering Audit

Completed:

- Added renderer-owned command-preparation audit coverage for serial and parallel execution.
- Added deterministic chunk-count and checksum parity checks for empty, small, 512+, ARPG-sized, and large workloads.
- Added the focused `RenderingAudit_Serial` Release benchmark case; parallel live recording remains intentionally outside the default gate.
- Simplified live renderer command recording to one serial secondary command buffer because the representative audit measured parallel preparation overhead above serial work; audit cases reported 0 B/op after warm-up.

Verification:

- Solution build passed with 0 errors and 0 warnings.
- Plain console smoke tests passed, including rendering-audit coverage.
- Release renderer audit cases reported 0 B/op and 0 gen0 collections.
- Normal ARPG and forced-parallel bounded Vulkan sample runs exited cleanly.
- `git diff --check` passed.

## 2026-08-13 — Milestone 12: Tune Multithreading

Completed:

- Added sparse scheduler execution-policy metadata for Serial, Adaptive, Parallel, and Background systems.
- Added optional per-system timing, item-count, allocation, GC, and parallel-decision diagnostics.
- Classified sample input/player movement as Serial, monster AI/movement and integration collision as Adaptive, and projectile combat as Adaptive with a 64-item threshold.
- Added policy benchmark cases while preserving the existing ARPG and ECS/query/job benchmark matrix.
- Ensured benchmark-owned JobSystem instances are disposed after execution.

Verification:

- Solution build passed with 0 errors and 0 warnings.
- Plain console smoke tests passed, including policy diagnostics and background-system tests.
- Release benchmark matrix passed; policy and ARPG cases reported 0 B/op and 0 gen0 collections.
- Normal ARPG and forced-parallel bounded sample runs exited cleanly.
- `git diff --check` passed.

## 2026-08-13 — Milestone 11: Realistic ARPG Benchmark

Completed:

- Added deterministic ARPG workload and camera-projected sample rendering.
- Added serial, adaptive-parallel, and forced-parallel benchmark cases.
- Added fixed-capacity effect reuse, animation state progression, projectile combat checks, and stable extraction coverage.
- Added bounded `--frames` sample execution for automated Vulkan verification.

Verification:

- Solution build passed with 0 errors and 0 warnings.
- Plain console smoke tests passed, including dedicated ARPG assertions.
- Release benchmark cases reported 0 B/op and 0 gen0 collections.
- Isometric and forced-parallel ARPG sample runs exited cleanly after 10 frames.
- `git diff --check` passed.

## 2026-08-13 — Milestone 10: Gameplay API

Completed:

- Added immutable hero, monster, weapon, skill, projectile, and item definitions in `Engine.App`.
- Added deferred `World.SpawnHero`, `SpawnMonster`, `SpawnProjectile`, and `SpawnItem` APIs with scene ownership and reserved entity handles.
- Added shared gameplay state components and migrated the sample through `SampleEntitySpawner`.

Verification:

- Deferred activation, component preservation, deterministic grouping, and scene cleanup passed in console coverage.
- Debug build, plain console tests, bounded isometric sample runs, Release benchmark comparison, direct-spawn search, and diff checks passed.

## 2026-08-13 — Milestone 9: Structural Command Buffer

Completed:

- Replaced `WorldCommandBuffer` with FIFO `EntityCommands` supporting deferred Create, Destroy, Add, Remove, Clear, and Apply operations.
- Added reserved-entity lifecycle support with generation validation and stale-operation safety.
- Migrated sparse sample callers and command-buffer smoke coverage.

Verification:

- Reserved entities remained hidden until Create was applied.
- Stale Remove/Destroy operations were harmless and reserved destruction recycled safely.
- Debug build, plain console tests, bounded isometric sample runs, Release benchmark comparison, API searches, and diff checks passed.

## 2026-08-13 — Milestone 8: Simplify JobSystem

Completed:

- Replaced the public execution surface with `Run`, `ParallelFor`, `Wait`, and `IsComplete`.
- Removed arbitrary dependency scheduling, waiting queues, packed dependency storage, and dependency-only tests.
- Preserved worker channels, work stealing, pooled chunks, slot reuse, parent-child parallel barriers, exception propagation, and the 4096-job capacity guard.
- Migrated renderer, tile extraction, texture loading, sparse queries, tests, and benchmarks.

Verification:

- Debug build and plain console smoke tests passed.
- Release benchmark comparison passed with 12 PASS, 0 WARN, 0 FAIL.
- Parallel-for range, exception, slot reuse, capacity, and sparse-query allocation coverage passed.

## 2026-08-13 — Milestone 7: Multithreaded Sparse Queries

Completed:

- Added opt-in one-, two-, and three-component sparse `ParallelForEach` queries.
- Added smallest-store chunk partitioning, measured serial fallback, static parallel callbacks, and pooled allocation-free dispatch after warm-up.
- Added serial/parallel parity, deterministic visitation, disjoint-row mutation, stale-entity, exception, fallback, and allocation coverage.
- Added serial/parallel sparse-query benchmarks for 100, 500, 1,000, 5,000, and 100,000 entities.

Verification:

- Debug build, plain console smoke tests, and `git diff --check` passed.
- Release benchmark comparison passed with 12 PASS, 0 WARN, 0 FAIL.

## 2026-08-12 — Milestone 6: Remove Archetype ECS

Completed:

- Deleted archetype storage, migration, cached-query, old scheduler, command-buffer, and parallel-dispatch infrastructure.
- Removed archetype-only tests and benchmarks; converted remaining runtime contracts to sparse entities.
- Sparse ECS is now the only ECS implementation.

Verification:

- Debug/Release builds, console smoke tests, bounded isometric sample launches, and benchmark comparison passed.

## 2026-08-12 — Milestone 5: Port IsometricSandbox to Sparse ECS

Completed:

- Migrated sample gameplay, rendering queries, scene ownership, restart cleanup, deferred mutation, and system execution to sparse ECS.
- Kept execution serial while preserving normal and simulation behavior.

Verification:

- Debug/Release builds, console smoke tests, isometric simulation launches, and benchmark comparison passed.

## 2026-08-12 — Milestone 4: Explicit Frame Scheduler

Implemented an additive explicit frame scheduler over the existing archetype ECS.

Completed:

- Added named `FrameStage`, `SystemRegistration`, `ParallelGroup`, `Barrier`, and `FrameScheduler` contracts.
- Added deterministic stage/order plan construction and diagnostics through the built plan.
- Added explicit parallel-group execution using `JobSystem` and conflict validation from declared access metadata.
- Preserved the existing `SystemScheduler`, sample wiring, renderer, sparse ECS, and command-buffer behavior.
- Added scheduler smoke tests and isolated `FrameScheduler_*` benchmarks.

Verification:

- `dotnet build Engine.slnx --nologo` — passed with 0 errors and 0 warnings.
- `dotnet run --project tests\Engine.Tests` — passed.
- Release benchmark comparison — 12 PASS, 0 WARN, 0 FAIL.

## 2026-08-12 — Milestone 3: Sparse ECS Queries

Implemented allocation-free serial dense-store queries for the sparse ECS.

Completed:

- Added struct/static callback contracts and `Query<T>`, `Query<T1,T2>`, and `Query<T1,T2,T3>`.
- Queries drive from the smallest required component store and pass component rows by reference.
- Added world query factories without caching or scheduler/parallel integration.
- Added intersection, mutation, structural-change, stale/destroy, empty, and allocation-free smoke coverage.
- Added `SparseQuery_*` benchmarks for 100, 500, 1,000, 5,000, and 100,000 entities.

Verification:

- `dotnet build Engine.slnx --nologo` — passed with 0 errors and 0 warnings.
- `dotnet run --project tests\Engine.Tests` — passed.
- Release benchmark comparison — 12 PASS, 0 WARN, 0 FAIL; sparse query benchmarks reported 0 B/op.

## 2026-08-12 — Milestone 2: New Sparse ECS Core

Implemented a sparse-set ECS core alongside the existing archetype ECS.

Completed:

- Added `Engine.Ecs.Sparse.Entity` with integer ID and generation validation.
- Added generation-safe `EntityRegistry` with ID recycling.
- Added dense/sparse `ComponentStore<T>` with swap-remove and stale-handle protection.
- Added sparse `World` component operations: create, destroy, add, remove, get, try-get, and has.
- Kept the implementation independent from archetypes, queries, scheduler, JobSystem, command buffers, scenes, and rendering.
- Added sparse ECS smoke tests, including steady-state allocation validation.
- Added isolated `SparseEcs_*` Release benchmarks without replacing the normal baseline.

Verification:

- `dotnet build Engine.slnx --nologo` — passed with 0 errors and 0 warnings.
- Plain console smoke tests — passed.
- Sparse ECS tests — passed, including generation recycling, swap-remove, capacity growth, destruction cleanup, and zero-allocation lookup.
- Release benchmark comparison — 12 PASS, 0 WARN, 0 FAIL; sparse benchmarks reported as new entries with 0 B/op.

Next planned milestone: Milestone 3 — Queries.

## 2026-08-12 — Milestone 0: Baseline Runtime Measurement

Implemented the benchmark-only baseline matrix for the current archetype ECS/runtime.

Completed:

- Added serial and parallel workloads for 100, 250, 500, 1,000, 5,000, and 100,000 entities.
- Reused the current `Query<T1,T2,T3>`, `ForEach`, `ForEachParallel`, `SystemScheduler`, and `JobSystem` APIs.
- Added simulation/frame timing, scheduler overhead, allocations, GC counters, jobs/frame, worker participation, and utilization reporting.
- Added `--milestone0`, `--workers`, and `--chunk-size` benchmark options.
- Wrote dedicated output to `benchmarks/results/milestone0.json` without changing the normal benchmark baseline.
- Added serial/parallel checksum validation and speedup interpretation.

Results:

- On the measured machine, serial execution was faster at every tested entity count with the default 31-worker JobSystem.
- Parallel execution used all available workers at larger counts, but scheduling and synchronization overhead exceeded the workload cost.
- These results support serial-by-default/adaptive parallelism and must not be treated as final thresholds for other machines.

Verification:

- Shortened matrix passed with matching checksums for all sizes.
- Full Release matrix passed and produced `benchmarks/results/milestone0.json`.
- Solution build and plain console smoke tests passed.
- The existing benchmark suite remains separately runnable; its baseline comparison is not part of the Milestone 0 result file.

## 2026-08-12 — Milestone 1: Game / World / Scene Contracts

Implemented the first architecture milestone from `Simplified Architecture v2.md`.

Completed:

- Added the public `Engine.App.Game` base contract with active-world ownership.
- Added the runtime `Engine.App.World` wrapper around the existing `Engine.Ecs.World`.
- Added `Scene` loading, switching, unloading, and active-scene tracking.
- Added `EntityLifetime.Scene`, `EntityLifetime.World`, and `EntityLifetime.Transient` classifications.
- Scene unload destroys scene-owned ECS entities while preserving world/transient entities.
- Wired the sample hierarchy as `GameHost → Game → Sanctuary → Forest`.
- Preserved the archetype ECS, system scheduler, JobSystem, renderer, and rendering extraction behavior.
- Added lifecycle smoke tests to the plain console test suite.

Verification:

- `dotnet build Engine.slnx --nologo` — passed with 0 errors and 0 warnings.
- `dotnet run --project tests\Engine.Tests` — passed.
- Simulation sample startup reached the interactive process without startup errors.
- Benchmark comparison was run; existing extraction, collision, and math baseline timing regressions were reported. This milestone does not modify those hot paths.

Next planned milestone: Milestone 2 — New Sparse ECS Core.
