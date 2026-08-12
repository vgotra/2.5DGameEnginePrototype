---
name: math
description: Applies math helper conventions: aggressive inlining and branchless programming with bitwise masks and precomputed lookup tables. Use when writing or reviewing math, simulation, or AI decision code in performance-sensitive projects. Do not use for tooling, docs, tests, or samples that relax hot-path rules.
---

# Math

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on math helpers.
- AVOID `if/else` in hot loops. USE branchless programming: bitwise masks and precomputed lookup tables.
