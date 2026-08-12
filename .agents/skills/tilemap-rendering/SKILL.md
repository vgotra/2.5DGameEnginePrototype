---
name: tilemap-rendering
description: Applies tile-grid rendering conventions: chunked world partitions, visible-chunk culling, static chunk buffers, dirty-tracking for edits, and band/chunk parallelism. Use when writing or reviewing tilemap rendering or chunk code in a 2D/2.5D engine. Do not use for sprite batching in general (see rendering-batching) or physics/collision (see collision-2d).
---

# Tilemap Rendering

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- SPLIT the world into fixed-size chunks. Render only chunks intersecting the camera bounds (SEE `camera-view`, `culling-spatial`).
- CACHE static chunks into static vertex buffers; rebuild a chunk only when it changes (dirty tracking), not every frame.
- BUDGET tile updates per frame; stream chunks asynchronously for large worlds.
- PARALLELIZE chunk processing with the job system using band/chunk partitions; merges happen on the main thread after the barrier. SEE `job-system`.
- USE SIMD for batch tile processing (position/UV computation). SEE `simd`.
- RENDER through the shared sprite batch/instance path. SEE `rendering-batching`.
