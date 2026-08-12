---
name: collision-2d
description: Applies 2D collision conventions: AABB/OBB/circle narrowphase, swept/continuous collision for fast movers, resolution, and reuse of a spatial broadphase. Use when writing or reviewing collision or physics-ish code in a 2D/2.5D engine. Do not use for native physics engines (see physics-native) or spatial queries (see culling-spatial).
---

# Collision (2D)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE a fixed timestep for all collision updates; resolve on the main thread for deterministic ordering. SEE `determinism`.
- USE the spatial broadphase to reduce candidate pairs; narrowphase runs only on candidates. SEE `culling-spatial`.
- SUPPORT AABB, OBB, and circle shapes; implement separating-axis resolution for oriented cases.
- USE swept/continuous tests for fast-moving entities to avoid tunneling; keep the cost budgeted.
- STORE collider state in value-type components keyed by Entity IDs, never managed references. SEE `ecs`, `physics-native`.
- PROCESS collision in dedicated systems that iterate spans/pointers, with zero allocation in the hot path. SEE `hot-path-interop`, `pre-generation-checklist`.
