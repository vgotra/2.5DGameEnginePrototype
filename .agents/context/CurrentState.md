# Current State

## Status

The engine is a Windows-first .NET 10 prototype for 2D/2.5D isometric games. The MVP vertical slice is complete: SDK/build policy, core entity and clock types, isometric math, ECS storage, backend-neutral contracts (rendering/audio/physics/platform), a deterministic tile map, continuous movement with collision and jump, camera following, Win32 window/input, and the Vulkan render path.

Rendering is split between a backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`) and a single Vulkan backend that draws white diamonds with black borders in iso and white boxes in `--2d`:

- **Vulkan (`Engine.Rendering.Vulkan`)** — swapchain, render pass, per-swapchain framebuffers, SPIR-V shape shaders (GLSL compiled with glslc, copied to `shaders/` in output), graphics pipeline, descriptor pool allocator, texture uploader, and a batch renderer that accumulates submitted packets and issues one indexed draw per frame. 

Windowing (`Engine.Platform.Win32`): the window opens centered on the primary screen at 800x600.

Vulkan is the only renderer.

The platform layer is cross-platform-ready. See `docs/LinuxSupportPlan.md`.

## Recent work

- **Hot-path conventions** (`docs/Conventions/HotPath.md`, added to the conventions index): zero-alloc hot paths, spans, `ref`/`in`/`out`, SoA, short inlinable methods, `[LibraryImport]` interop rules. All generated code follows it.
- **Hot-path pass applied to existing code**: `IsometricCamera` now exposes a per-frame `ScreenTransform` (Origin/Scale) hoisted out of `RenderExtractionSystem`'s tile loop (per-tile cost is now two scalar FMAs per axis, no instance calls/branches/temp allocations); `WorldToScreen` reimplemented via the transform (bit-identical for the camera tests); `[MethodImpl(AggressiveInlining)]` added to short math/collision/input methods (`IsometricMath`, `TileMap`, `MovementSystem`, `Win32Input`); `BatchRenderer` swapped `List<T>` for a preallocated `GrowableBuffer<T>` and split `AddShape` into short box/diamond methods; `SparseSet<T>` rewritten from `Dictionary<uint,int>` + lists to a dense/sparse SoA (sparse `int[]` → packed `EntityId[]` + `T[]`, generation-checked, swap-with-last). All 40 smoke tests pass; sample runs clean in iso and `--2d`.
- `SparseSet` growth is amortized-doubling (allocates on growth, like `List<T>`); steady-state add/tryget/remove are allocation-free.
- **Benchmark/metrics instrumentation**: `benchmarks/Engine.Benchmark` — a BCL-only Release console harness for the CPU hot paths (extraction iso/flat 20×20 + 128×128, tile-map collision/movement, `SparseSet`, `GrowableBuffer`, `ScreenTransform`/`IsometricMath`). Each benchmark warms up, then measures exact per-thread alloc bytes/op + gen0/1/2 collection deltas and a 7-trial median ns/op. Results: `benchmarks/results/last.json` (every run, gitignored) and `baseline.json` (`--save`, committed). `--compare last|baseline` prints a delta table — time WARN ≥ +15%, FAIL ≥ +30%; allocations FAIL above `--alloc-tolerance` (default 0.5 B) or on any gen0; exit code 1 on same-machine FAIL. Docs: `docs/RunningAndVerifying/Benchmarking.md`; `Verify.md` gains a bench step for hot-path changes. The sample gained `--metrics` (rolling table every 120 frames: avg/max ms, sim steps, sprites, avg B/frame, GC collections). All 40 smoke tests pass; bench runs clean (steady-state 0 B/op).
