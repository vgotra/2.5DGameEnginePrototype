---
name: memory-spans
description: Applies memory and span-processing conventions: unsafe/raw pointers, Span/ReadOnlySpan/Memory, NativeMemory alloc/free (no Marshal.AllocHGlobal), by-ref struct passing, and no LINQ/foreach in hot paths. Use when writing or reviewing memory-sensitive or hot-path code. Do not use for tooling, docs, tests, or samples that relax these rules.
---

# Memory & Span Processing

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- USE `unsafe` blocks and raw pointers (`*`) for rendering and grid logic.
- USE `Span<T>`, `ReadOnlySpan<T>`, and `Memory<T>`.
- USE `NativeMemory.Alloc` and `NativeMemory.Free` for unmanaged heaps. FORBID `Marshal.AllocHGlobal`.
- USE `ref`, `in`, `out`, and `ref readonly` to avoid struct copying.
- FORBID LINQ and `foreach` in hot paths. USE standard `for` loops.
