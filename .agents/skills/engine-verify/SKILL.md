---
name: engine-verify
description: Runs the engine's post-change verification loop: build, smoke tests, sample boot with flags, and the benchmark gate. Use after any code or shader change to confirm nothing regressed. Do not use for documentation-only edits, or when writing new code (that is engine-development work).
---

## What I do

Run the ordered verification checklist from `docs/RunningAndVerifying/Verify.md` (the source of truth). Always run from the repo root.

1. **Build** — `dotnet build Engine.slnx --nologo`. Requires 0 errors; warnings don't matter.
2. **Smoke tests** — `dotnet run --project tests\Engine.Tests\Engine.Tests.csproj`. A plain console app, NOT a test framework; do NOT use `dotnet test`. Success = the app prints `Smoke tests passed`.
3. **Sample run** — `dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj`. Include every flag combo the change touched (`--2d`, `--cap 60`, `--fullscreen`, `--metrics`). Confirm rendering, fullscreen toggle (F11), drag-resize, and `Escape` exit.
4. **Benchmark gate** — ONLY when the change touches hot paths: `dotnet run -c Release --project benchmarks\Engine.Benchmark\Engine.Benchmark.csproj -- --compare baseline`. Should show no FAILs and no new allocations (Release-only, no GPU).

## Rules

- If a step fails, fix it before moving on; do not declare verification passed.
- `--metrics`/allocation and pacing expectations: `docs/RunningAndVerifying/Benchmarking.md`.
