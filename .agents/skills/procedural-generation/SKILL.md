---
name: procedural-generation
description: Applies procedural generation conventions: seeded deterministic noise (Perlin/Simplex/Voronoi), seeded PRNGs, worker-thread generation, and reproducible tile/world output. Use when writing or reviewing procedural content, noise, or world/tilemap generation code in a game engine. Do not use for scene loading/serialization (see scenes-memory) or pathfinding (see pathfinding).
---

# Procedural Generation

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- GENERATE from an explicit seed; identical seed + input yields identical output. SEE `determinism`.
- USE deterministic PRNGs and noise (Perlin/Simplex/Voronoi); NEVER `Random`/wall-clock in generation. SEE `determinism`.
- RUN generation on worker threads and stream results in chunks for large worlds. SEE `job-system`, `tilemap-rendering`.
- BATCH noise/PRNG evaluation with SIMD where it pays off. SEE `simd`.
- PIPE generated tiles through the scene/tilemap serialization path without managed allocation. SEE `scenes-memory`.
