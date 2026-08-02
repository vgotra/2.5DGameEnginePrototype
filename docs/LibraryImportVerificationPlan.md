# LibraryImport Migration — Optional Verification Plan

Optional, non-blocking verification for roadmap item #14 (`[DllImport]` → source-generated `[LibraryImport]` in `Engine.Platform.Win32`). All tiers below are **Not started**; pick any subset. None of them block other roadmap work.

## Purpose and reality check

The migration's payoff is **non-functional**: AOT/trim-readiness and build-time marshalling validation. It is **not** a runtime speedup. The Win32 backend P/Invokes ~10–15 times per frame (the `PeekMessageA` drain loop, 7× `GetAsyncKeyState`, `IsWindow`, plus close/destroy on exit). Marshalling overhead per blittable call is low-nanoseconds; the four string-bearing APIs (`CreateWindowExW`/`SetWindowTextW`/`GetMonitorInfoW`/`GetModuleHandleW`) are ~50–100 ns but fire only on startup/title-set/fullscreen-toggle, not per frame. Total per-frame P/Invoke cost is ~1–5 µs. `[LibraryImport]` saves a fraction of that — ~0.5–2 µs per frame — versus `vkQueueSubmit`/`vkAcquireNextImageKHR` measuring in the hundreds of µs.

**Expected outcome for every tier**: "no measurable runtime regression; AOT/trim claim holds." Documenting that null result is itself the verification product.

## Tier A — External runtime profiling

**Scope**: documents no-runtime-regression baseline. No code/repo changes.
**Prereq**: `dotnet tool install -g dotnet-counters dotnet-trace` (one-time, system-level install to `%USERPROFILE%\.dotnet\tools`). Approved on execution.
**归属**: roadmap item #14 follow-up.

Steps:
1. Build two binaries for comparison:
   - Pre-migration: `git worktree add ../pre-mig <commit-before-item-14>` then `dotnet build` there.
   - Post-migration: current `main` build.
2. Launch `IsometricSandbox.exe` (the built apphost, not `dotnet run`) in each tree.
3. Attach `dotnet-counters monitor --process-id <pid> --counters System.Runtime --refresh-interval 1` for ~60 s of steady-state. Capture: gen-0/1/2 collection counts, time-in-GC %, allocation rate, working set, CPU usage.
4. Attach `dotnet-trace collect --process-id <pid> --profile cpu-sampling --duration 00:00:10` for a 10 s CPU sample. Open the `.nettrace` in PerfView or `speedscope.app`. Confirm Win32 marshalling is invisible in the flame graph (well below 0.1% of CPU).
5. RenderDoc frame capture (already wired via the `renderdoc` MCP server) for GPU-side frame timing — proves the migration didn't disturb the render path.
6. Record results inline in this doc (append a "Results" section per tier).

**Pass criterion**: pre- and post-migration counters within ±5% on every metric; no new hot path appears in the flame graph.

## Tier B — AOT publish verification

**Scope**: proves the migration's AOT/trim claim — the actual non-functional payoff. The real point of item #14.
**Prereq**: none (uses shipped .NET 10 SDK).
**归属**: roadmap item #14 follow-up.

Steps:
1. Verify csproj readiness. `Engine.Platform.Win32.csproj` may need `<IsAotCompatible>true</IsAotCompatible>`; the sample needs `<PublishAot>true</PublishAot>` (or pass `/p:PublishAot=true` on the CLI). Read first; do not assume.
2. Try `dotnet publish samples\IsometricSandbox\IsometricSandbox.csproj -c Release -r win-x64 /p:PublishAot=true -o .\publish-aot`.
3. **Fallback scope** (likely needed): `Vortice.Vulkan` is a large wrapper and may not be AOT-clean yet, which would block a sample publish. If so, isolate the claim to our migration by writing a tiny AOT test app (or adding a `--aot-self-test` flag to the existing sample that exits before the Vulkan path) that only opens a `Win32Window`, pumps messages for 2 s, and closes. Publish THAT under AOT. This isolates "our P/Invoke surface is AOT-clean" from "Vortice is AOT-clean."
4. Confirm the publish emits **0 trimmer warnings** (`PublishAot` implies `ILLink`/trimming).
5. Launch the AOT binary, run 10 s, `CloseMainWindow`, confirm exit code 0.
6. Compare binary size and cold-start time vs the JIT-launched sample (`IsometricSandbox.exe` from `dotnet build`). Record numbers.

**Pass criterion**: clean publish (0 warnings), app launches, exits cleanly. Expected trimmer warnings on the pre-migration tree (or a failed publish) confirm the migration mattered.

## Tier C — BenchmarkDotNet microbench

**Scope**: quantifies per-call marshalling overhead. Overkill for one migration; really the scope of roadmap item #5.
**Prereq**: add `BenchmarkDotNet` to `Directory.Packages.props`; new `tests/Engine.Benchmarks` project.
**归属**: roadmap item #5 (forward-linked here for completeness).

Steps:
1. `dotnet new classlib -o tests/Engine.Benchmarks` (or console app — BenchmarkDotNet needs a runnable project).
2. Add `<PackageReference Include="BenchmarkDotNet" />` (no version; central package management).
3. Reference the `Engine.Platform.Win32` project.
4. Write benchmarks for the actual Win32 declarations: `PeekMessageA`, `TranslateMessage`, `DispatchMessageA`, `GetAsyncKeyState`, `GetForegroundWindow`, `IsWindow`, `GetSystemMetrics`. Provide both `[LibraryImport]` and `[DllImport]` versions side-by-side (the migration removed the `[DllImport]` forms — keep them in a benchmark-only file for comparison).
5. `dotnet run -c Release --project tests/Engine.Benchmarks` — BenchmarkDotNet prints mean/error/stddev per call.
6. **Expected result**: LibraryImport is faster by single-digit nanoseconds per call on blittable signatures; the gap is wider on string-bearing APIs but those aren't per-frame.

**Pass criterion**: benchmark prints; results recorded in this doc.

## Tier D — In-engine telemetry

**Scope**: lightweight in-engine perf instrumentation. Properly the scope of roadmap item #5.
**Prereq**: code changes to `FrameTimer` and `Program.cs`.
**归属**: roadmap item #5 (forward-linked here for completeness).

Steps:
1. Extend `Engine.Core.FrameTimer` with rolling aggregates: avg, p99, min, max over a rolling window (e.g., 120 frames). Print to console every N frames or when a `--profile` flag is set.
2. Add `GC.GetAllocatedBytesForCurrentThread()` and `GC.CollectionCount(0|1|2)` deltas to the sample hot loop (`Program.cs`); report at the same cadence as frame stats.
3. Optional `--profile` flag on the sample that enables both.
4. Document the metrics format in `docs/PerformanceBudget.md`.

**Pass criterion**: `dotnet run --project samples/IsometricSandbox -- --profile` prints rolling frame stats and GC deltas without warnings; code stays 0-warning under `TreatWarningsAsErrors`.

## Verification matrix

| Tier | Prereq | Code change | Expected result |归属 |
|------|--------|-------------|-----------------|---------|
| A — External profiling | `dotnet tool install -g dotnet-counters dotnet-trace` | None | Pre/post within ±5%; no new hot path | #14 |
| B — AOT publish | None (uses .NET 10 SDK) | Possibly 1 csproj line | Clean publish + clean run/exit | #14 |
| C — BenchmarkDotNet | New package + test project | New bench file | Per-call ns delta recorded | #5 |
| D — In-engine telemetry | None | `FrameTimer` + `Program.cs` | `--profile` prints frame + GC stats | #5 |

## Status

- All tiers: **Optional / Not started**.
- No tier blocks any other roadmap item.
- Results are recorded inline (append a "## Results — Tier X" section per executed tier) so this doc becomes the historical record.

## Cross-references

- Roadmap item #14 (DONE): `docs/Roadmap.md`.
- Migration commit / release note: `RELEASE_NOTES.md` 2026-08-03.
- Profiling roadmap item: `docs/Roadmap.md` item #5.
- Frame pacing context (the `FrameTimer` Tier D would extend): `docs/FramePacingPlan.md`.
- RenderDoc GPU capture workflow: `docs/RenderDocSetup.md`.