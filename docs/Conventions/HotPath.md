# Hot Path, AI Agent, and Native Interop Conventions

- ENFORCE zero allocation in the hot path. NEVER `new` a reference type in `Update`/`Tick`/per-frame code. USE stack `struct`s and preallocated buffers.
- ITERATE with `Span<T>`/`ReadOnlySpan<T>` when scanning pathfinding nodes, target arrays, or agent pools. Slicing is a view, not a copy.
- PASS by reference: `ref` for mutating structs, `in` for read-only structs larger than 16 bytes, `out` for results. NEVER pass large structs by value.
- ENFORCE data-oriented design (SoA): store agent state as one contiguous array per field (`PositionX[]`, `Health[]`, `TargetId[]`).
- ENFORCE `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on short math, distance, and AI decision methods.
- SEPARATE data from logic: pure agent data (vectors, timers, target IDs) lives in value-type structs; processing lives in static methods or singleton system classes that take those structs by `ref`.
- KEEP methods short (under ~20-30 lines). The JIT/AOT inliner refuses large methods and methods with complex `try`/`catch`.
- MINIMIZE branching in hot loops. AVOID large `if`/`else if` chains for AI state evaluation. PREFER math, bitwise operations, and precomputed masks/tables.
- ENFORCE `[LibraryImport]` for OS/native-C P/Invoke (OS windowing interop such as SDL3-CS's bindings, native C libraries such as JoltC). NEVER `[DllImport]` — the source generator emits marshalling at compile time, eliminating runtime marshalling stubs. BIND Vulkan through the Vortice.Vulkan managed package instead.
- DECLARE the native surface as `internal static partial` methods in one file per backend.
- USE blittable types only: pointers (`void*`, `nint`, pointer-to-struct) instead of C# arrays. NEVER pass managed objects.
- USE `unsafe`: native wrapper methods live in `unsafe` contexts and take pointers.
- SKIP `SetLastError` (default under `[LibraryImport]`); Vulkan reports failures through its own `VkResult`, not OS error codes.
- USE `[SuppressGCTransition]` ONLY where it is safe: extremely fast, non-blocking calls that run in well under a microsecond. NEVER apply it to calls that take locks, call back into managed code, or run long — skipping the GC transition there risks deadlock or uncooperative thread suspension.
- KEEP interop behind the platform seams. Bindings live in the platform/Vulkan backends, never in shared contracts. SEE `Restrictions.md`.
