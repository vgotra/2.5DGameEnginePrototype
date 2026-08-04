# Memory & Span Processing

- USE `unsafe` blocks and raw pointers (`*`) for rendering and grid logic.
- USE `Span<T>`, `ReadOnlySpan<T>`, and `Memory<T>`.
- USE `NativeMemory.Alloc` and `NativeMemory.Free` for unmanaged heaps. FORBID `Marshal.AllocHGlobal`.
- USE `ref`, `in`, `out`, and `ref readonly` to avoid struct copying.
- FORBID LINQ and `foreach` in hot paths. USE standard `for` loops.
