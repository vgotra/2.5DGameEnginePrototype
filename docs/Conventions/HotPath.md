# Hot Path, AI Agent, and Native Interop Conventions

Rules for maximising performance where the CPU matters most: per-frame/`Update`/`Tick` loops, large-scale AI simulation, and the Vulkan P/Invoke surface. General allocation rules are in [`Coding.md`](Coding.md); this file adds structure and interop rules.

## Hot path optimization

- **Zero allocation in the hot path.** Never `new` a reference type in `Update`/`Tick`/per-frame code. Temporary data goes on the stack in `struct`s; persistent data comes from preallocated buffers.
- **Iterate with spans.** Use `Span<T>`/`ReadOnlySpan<T>` when scanning pathfinding nodes, target arrays, or agent pools. Slicing is a view, not a copy.
- **Pass by reference.** Use `ref` for mutating structs, `in` for read-only structs larger than 16 bytes, and `out` for results. Avoid passing large structs by value.
- **Data-oriented design (SoA).** Store agent state as structures of arrays (one contiguous array per field — `PositionX[]`, `Health[]`, `TargetId[]`). The CPU walks contiguous memory instead of chasing pointers between class objects.
- **Aggressive inlining.** Put `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on short math, distance, and AI decision methods.

## Class and method splitting

- **Separate data from logic.** Pure agent data (vectors, timers, target IDs) lives in value-type structs. Processing lives in static methods or singleton system classes that take those structs `by ref`.
- **Keep methods short** (under ~20-30 lines). The JIT/AOT inliner refuses large methods and methods with complex `try`/`catch`.
- **Minimize branching in hot loops.** Avoid large `if`/`else if` chains for AI state evaluation (e.g. Utility AI weights). Prefer math, bitwise operations, and precomputed masks/tables.

## `[LibraryImport]` for Vulkan / native interop

`[LibraryImport]` is the standard for native bindings: the source generator emits marshalling at compile time, eliminating the runtime marshalling stubs of `[DllImport]`. The repo already follows this in `Engine.Platform.Win32.NativeMethods`.

- **Declare partial.** Native surface is `internal static partial` methods in one file per backend.
- **Blittable types only.** Pass pointers (`void*`, `nint`, pointer-to-struct) instead of C# arrays; never pass managed objects.
- **Use `unsafe`.** Native wrapper methods live in `unsafe` contexts and take pointers.
- **Skip error marshalling.** Omit `SetLastError` (default under `[LibraryImport]`); Vulkan reports failures through its own `VkResult`, not OS error codes.
- **`[SuppressGCTransition]` only where it is safe.** Use it for extremely fast, non-blocking calls (e.g. basic Vulkan setup/getters that run in well under a microsecond and never block the thread). Do not apply it to calls that could take locks, call back into managed code, or run long — skipping the GC transition on those risks deadlock or uncooperative thread suspension.
- **Keep interop behind the platform seams.** Bindings live in the platform/Vulkan backends, never in shared contracts (`Restrictions.md`).
