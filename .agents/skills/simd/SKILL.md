---
name: simd
description: Applies SIMD conventions: hardware intrinsics (System.Runtime.Intrinsics, Vector128<T>/Vector256<T>) for batch tile and physics processing. Use when writing or reviewing batch-processing or physics code in a game engine. Do not use for scalar hot paths or non-performance-critical code.
---

# SIMD

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE hardware intrinsics (`System.Runtime.Intrinsics`, `Vector128<T>`, `Vector256<T>`) for batch tile and physics processing.
