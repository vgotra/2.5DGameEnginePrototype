# Completed Milestones

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
