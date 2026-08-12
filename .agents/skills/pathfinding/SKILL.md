---
name: pathfinding
description: Applies pathfinding and agent navigation conventions: grid A* and flow fields, deterministic path ordering, path caching and invalidation, and steering. Use when writing or reviewing pathfinding, AI navigation, or agent steering code in a 2D/2.5D engine. Do not use for decision/state machines (see state-machines) or physics (see collision-2d).
---

# Pathfinding & Agent Navigation

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- USE grid A* (or flow fields for many agents) on the navigation grid; prefer precomputed fields when the grid is static.
- ENSURE deterministic results: stable tie-breaking and ordering, never hash/dictionary order. SEE `determinism`.
- CACHE computed paths; invalidate on world/nav-grid changes, not per frame.
- RUN pathfinding on worker threads; agents consume results on the main thread. SEE `job-system`.
- STEER agents along paths with branchless, vectorized math where possible. SEE `math`, `simd`.
- SCAN candidates with spans and preallocated open/closed buffers; zero allocation in the hot path. SEE `hot-path-interop`, `memory-spans`.
