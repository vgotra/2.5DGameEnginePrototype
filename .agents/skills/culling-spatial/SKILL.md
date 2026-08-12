---
name: culling-spatial
description: Applies spatial partitioning and culling conventions: uniform grids and quadtrees for 2D broadphase, range/ray queries, SoA storage, and incremental rebuilds with zero per-frame allocation. Use when writing or reviewing spatial index, broadphase, or visible-set code in a 2D/2.5D engine. Do not use for collision resolution itself (see collision-2d) or tilemap chunking (see tilemap-rendering).
---

# Spatial Culling & Broadphase

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- CHOOSE the partition by density: uniform grid for even distributions, quadtree for clustered/sparse content.
- SERVE range and ray queries to collision and camera/visible-set consumers. SEE `collision-2d`, `camera-view`.
- STORE indices/IDs in SoA arrays, not object references. SEE `hot-path-interop`, `ecs`.
- REBUILD incrementally on entity/component changes; do not rebuild the index per frame.
- KEEP query and rebuild paths allocation-free in steady state. SEE `memory-spans`, `pre-generation-checklist`.
- TIE broadphase output to the fixed-step collision resolution for deterministic ordering. SEE `determinism`.
