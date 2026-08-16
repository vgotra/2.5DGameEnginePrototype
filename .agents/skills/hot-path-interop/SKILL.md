---
name: hot-path-interop
description: Applies hot-path, agent, and native-interop conventions: zero allocation in per-frame code, span iteration, by-ref struct passing, SoA layout, aggressive inlining, short methods, branchless logic, [LibraryImport] P/Invoke, blittable-only natives, and interop kept behind platform seams. Use when writing or reviewing performance-critical or P/Invoke code. Do not use for tooling, docs, tests, or samples that relax hot-path rules.
---

# Hot Path, AI Agent, and Native Interop

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying.

## Hot path
- ENFORCE zero allocation in the hot path. NEVER `new` a reference type in `Update`/`Tick`/per-frame code. USE stack `struct`s and preallocated buffers.
- ITERATE with `Span<T>`/`ReadOnlySpan<T>` when scanning pathfinding nodes, target arrays, or agent pools. Slicing is a view, not a copy.
- PASS by reference: `ref` for mutating structs, `in` for read-only structs larger than 16 bytes, `out` for results. NEVER pass large structs by value.
- ENFORCE data-oriented design (SoA): store state as one contiguous array per field (`PositionX[]`, `Health[]`, `TargetId[]`).
- ENFORCE `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on short math, distance, and AI decision methods.
- SEPARATE data from logic: pure data (vectors, timers, target IDs) lives in value-type structs; processing lives in static methods or singleton system classes that take those structs by `ref`.
- KEEP methods short (under ~20-30 lines). The JIT/AOT inliner refuses large methods and methods with complex `try`/`catch`.
- MINIMIZE branching in hot loops. AVOID large `if`/`else if` chains for AI state evaluation. PREFER math, bitwise operations, and precomputed masks/tables.
- KEEP identifiers descriptive and extract protocol, layout, scale, and threshold literals into named constants. Optimization, span processing, and branch reduction must not make intent cryptic.

## Native interop
- ENFORCE `[LibraryImport]` for OS/native-C P/Invoke. NEVER `[DllImport]` — the source generator emits marshalling at compile time, eliminating runtime marshalling stubs.
- DECLARE the native surface as `internal static partial` methods in one file per backend.
- USE blittable types only: pointers (`void*`, `nint`, pointer-to-struct) instead of C# arrays. NEVER pass managed objects.
- USE `unsafe`: native wrapper methods live in `unsafe` contexts and take pointers.
- DECLARE native interop structs with `[StructLayout(LayoutKind.Sequential)]` or `Explicit` so field layout is deterministic and matches the native ABI.
- SKIP `SetLastError` (default under `[LibraryImport]`); the graphics API reports failures through its own result codes, not OS error codes.
- ALWAYS return and evaluate the native/graphics result value. FORBID C# exceptions for native errors; convert the result code at the boundary. SEE `coding-runtime`.
- USE `[SuppressGCTransition]` ONLY where it is safe: extremely fast, non-blocking calls that run in well under a microsecond. NEVER apply it to calls that take locks, call back into managed code, or run long — skipping the GC transition there risks deadlock or uncooperative thread suspension.
- ENABLE debug-only validation layers, disabled in Release, for zero shipping cost.
- BIND graphics APIs through their managed packages (e.g. the graphics-API .NET bindings); reserve `[LibraryImport]` for OS/native-C interop.
- KEEP interop behind the platform seams. Bindings live in the platform/graphics backends, never in shared contracts. SEE the `platform-neutrality` skill.
