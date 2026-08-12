---
name: profiling-diagnostics
description: Applies in-app profiling conventions: frame-time metrics, counters, allocation tracking, and diagnostic markers, with a ~0 B/frame steady-state target. Use when adding or reviewing in-app metrics, diagnostics, or performance telemetry. Do not use for the external benchmark harness gate (see build-and-verify) or for general logging (see logging).
---

# Profiling & Diagnostics

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`; concrete flags are in `.agents/context/ProjectConfig.md`.

## Rules
- TRACK steady-state metrics in-app: average/max frame time, fixed-step count, object/sprite count, allocated bytes/frame, and GC collection counts.
- MEASURE allocations with per-thread counters (e.g. `GC.GetAllocatedBytesForCurrentThread`) and gen0/1/2 collection deltas.
- TARGET ~0 B/frame and 0 collections in steady state; expect spikes only at first frames and on resize/swapchain rebuild.
- EXPOSE a metrics flag on the sample/app (e.g. `--metrics`) that prints a rolling summary table at a fixed interval; run it with a frame cap (e.g. `--cap 60`) so pacing is measurable alongside ~16.6 ms and ~0 B/frame.
- KEEP diagnostics zero-cost when disabled; guards must not allocate.
- EMIT structured markers/counters for external tools — `dotnet-counters` (runtime/GC counters), `dotnet-trace` / PerfView (ETW CPU + GC traces), `dotnet-gcdump` (heap snapshots), the platform profiler — without per-frame allocation. SEE `logging`.
- NOTE that CPU usage percentages are not a reliable in-app signal when the driver busy-threads under a frame cap; trust measured timing and allocation instead.
- GATE perf verdicts on same-machine comparisons only. SEE `build-and-verify`.