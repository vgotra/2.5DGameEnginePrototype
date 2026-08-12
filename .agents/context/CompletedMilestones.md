# Completed Milestones

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
