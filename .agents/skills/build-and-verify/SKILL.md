---
name: build-and-verify
description: Runs the post-change verification loop: prerequisites, build, smoke tests, sample run with flags, and the benchmark gate. Use after any code or shader change to confirm nothing regressed. Do not use for documentation-only edits, or when writing new code (that is development work).
---

# Build and Verify

Run the ordered checklist below after any change. Always run from the repository root. Substitute the `<...>` placeholders with this repo's actual project names from `.agents/context/ProjectConfig.md`.

## Prerequisites
- The platform is Windows-first (Linux/macOS planned). SEE `platform-neutrality`.
- The .NET SDK version is pinned by `global.json`; projects target the pinned framework with the configured `LangVersion`.
- The graphics API SDK (`<GraphicsApi>`) is required to run the sample (`vulkan-1.dll`); its shader compiler is auto-detected at build for shader recompiles, and builds still succeed with committed bytecode when it is absent. SEE `shader-workflow`.

## Ordered checklist
1. **Build** — `dotnet build <SolutionName>.slnx --nologo`. Requires 0 errors; warnings don't matter.
2. **Smoke tests** — `dotnet run --project <TestProject>`. A plain console app, NOT a test framework; do NOT use `dotnet test`. Success = the app prints `Smoke tests passed`. The suite exercises the core types (clock, camera, frame timer), tile-map collision, and the ECS world (entity recycle/purge, component add/remove/set/get/has, queries 1/2/3 serial + parallel parity and determinism, command-buffer apply, scheduler conflict ordering) plus the job-system drain under varied worker counts.
3. **Sample run** — run the ARPG sample with a positive frame limit and no frame pacing, for example `dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --frames 60 --cap 0`. Also run the separate simulation executable when simulation or benchmark code changes: `dotnet run --project samples\IsometricSimulation\IsometricSimulation.csproj -- --frames 10` and, when relevant, `dotnet run --project samples\IsometricSimulation\IsometricSimulation.csproj -- --parallel --frames 10`. Never run either executable unbounded during automated or tool-driven verification. Flags, controls, tunables, and their defaults are in `.agents/context/ProjectConfig.md`.

   In PowerShell, pass each flag and value as a separate argument. Use an argument array for scripted runs, for example `$sampleArgs = @('--frames', '10', '--cap', '0'); dotnet run --project <SampleProject> -- $sampleArgs`. Do not pass multiple flags as one quoted string; the sample parser will treat that as an unknown argument and the run will remain interactive with the default frame cap.
4. **Benchmark gate** — ONLY when the change touches hot paths: `dotnet run -c Release --project <BenchmarkProject> -- --compare baseline`. Should show no FAILs and no new allocations (Release-only, no GPU).

## Benchmark methodology
- The benchmark project is a plain console app, Release-only, no GPU — it runs CPU hot-path benchmarks. It runs warmup passes outside measurement (JIT/tiered compilation settle there), then a single-threaded allocation pass (exact per-thread bytes via `GC.GetAllocatedBytesForCurrentThread`, plus gen0/1/2 `GC.CollectionCount` deltas) and a 7-trial timing pass reported as median/min/max ns per op.
- Verdicts: **time** WARN at `+15%`, FAIL at `+30%` vs the reference median; **allocations** FAIL when average bytes/op exceeds the alloc tolerance (default `0.5` B) or any gen0 collection occurs. Steady-state target is **0 B/op, 0 collections**.
- Only same-machine comparisons are authoritative (results record machine name, commit, UTC timestamp). The process exit code is 1 when any benchmark FAILs on the same machine (usable in CI).
- Results files: `<ResultsDir>/last.json` (every run, gitignored) and `<ResultsDir>/baseline.json` (`--save`, committed known-good reference).
- Commands/options: `--save`, `--compare <last|baseline|none>`, `--iterations <count>`, `--machine <name>`, `--alloc-tolerance <bytes>`, `--help`.

## Rules
- If a step fails, fix it before moving on; do not declare verification passed.
- Concrete project names, flags, tolerances, and the benchmark command are in `.agents/context/ProjectConfig.md`.
- SKIP step 4 for changes that do not touch hot paths (docs, non-hot-path refactors).
