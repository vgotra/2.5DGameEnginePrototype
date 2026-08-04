# Benchmarking

How to measure performance and allocation behavior, and how to catch regressions across sessions (previous run vs current run, or vs a committed baseline).

## Headless benchmark harness

`benchmarks/Engine.Benchmark` is a plain console app (like the smoke tests — **not** a test framework). It runs **Release-only, no GPU** CPU hot-path benchmarks:

- `Extraction_Iso20x20` / `Extraction_Flat20x20` / `Extraction_Iso128x128` / `Extraction_Flat128x128` — `RenderExtractionSystem.ExtractMapSprites`
- `Collision_TryMoveOpen` / `Collision_TryMoveBlocked` / `Movement_Move` — `TileMap` collision + `MovementSystem`
- `SparseSet_AddRemove` / `SparseSet_TryGetHit` / `SparseSet_RemoveMiss`
- `Buffer_AddClear` / `Buffer_Add64Clear` — `GrowableBuffer<T>`
- `Math_ScreenTransformIso` / `Math_ScreenTransformFlat` / `Math_IsometricWorldToScreen`

Each benchmark runs warmup passes outside measurement (JIT/tiered compilation and first-time growth settle there), then a single-threaded allocation pass (exact per-thread bytes via `GC.GetAllocatedBytesForCurrentThread()`, plus gen0/1/2 `GC.CollectionCount` deltas) and a 7-trial timing pass reported as **median/min/max ns per op**.

## Commands (run from the repo root)

```powershell
# Run and print results (writes benchmarks/results/last.json)
dotnet run -c Release --project benchmarks\Engine.Benchmark\Engine.Benchmark.csproj

# Run and update the committed baseline
dotnet run -c Release --project benchmarks\Engine.Benchmark\Engine.Benchmark.csproj -- --save

# Compare current run against the committed baseline
dotnet run -c Release --project benchmarks\Engine.Benchmark\Engine.Benchmark.csproj -- --compare baseline

# Compare current run against the previous run (default)
dotnet run -c Release --project benchmarks\Engine.Benchmark\Engine.Benchmark.csproj -- --compare last
```

Options: `--save`, `--compare <last|baseline|none>`, `--iterations <count>` (override), `--machine <name>`, `--alloc-tolerance <bytes>`, `--help`.

## Results and comparison

- `benchmarks/results/last.json` — written on every run (gitignored).
- `benchmarks/results/baseline.json` — written on `--save` (committed known-good reference).

Each result records the machine name, git commit, and a UTC timestamp. **Only same-machine comparisons are authoritative** — absolute numbers vary with CPU frequency scaling and background load, so cross-machine deltas are advisory only (printed with that warning).

Verdicts per benchmark:

- **Time** — WARN at `+15%`, FAIL at `+30%` vs the reference median.
- **Allocations** — FAIL when average bytes/op exceeds `--alloc-tolerance` (default `0.5` B) or any gen0 collection occurs. Steady-state target is **0 B/op, 0 collections**.

The process exit code is 1 when any benchmark FAILs on the same machine (usable in CI).

## Live in-app metrics

`--metrics` on the sample prints a rolling table every 120 frames: average/max frame time, fixed-step count, sprite count, average allocated bytes/frame, and GC collections.

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --metrics --cap 60
```

Steady-state target: ~0 B/frame, 0 collections, ~16.6 ms pacing at `--cap 60`. Expected spikes: the first frames (buffer/snapshot growth) and resize (swapchain rebuild + snapshot regrowth). Note CPU% is not a reliable signal here — the capped-run busy-driver-thread known issue makes it misleading (see `.agents/context/KnownIssues.md`).

## External tools

For whole-process validation beyond the harness: `dotnet-counters` (runtime/GC rate counters), `dotnet-trace` / PerfView (ETW CPU + GC traces), `dotnet-gcdump` (heap snapshots), the Visual Studio profiler, and RenderDoc (GPU frame time, draw calls — see `RenderDocSetup.md`).
