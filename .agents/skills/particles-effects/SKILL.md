---
name: particles-effects
description: Applies 2D particle-system conventions: pooled particles, GPU-batched rendering, deterministic updates, and zero-allocation spawn/update in the hot path. Use when writing or reviewing particle or visual-effects code in a 2D/2.5D engine. Do not use for general pooling (see object-pooling) or sprite batching (see rendering-batching).
---

# Particles & Visual Effects

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- POOL particles at fixed capacity; acquire on spawn, release on death, never allocate per particle. SEE `object-pooling`.
- UPDATE particles in systems with SoA state (position, velocity, life) and batch math. SEE `hot-path-interop`, `simd`.
- RENDER particles through the sprite/instance batch, not per-particle draw calls. SEE `rendering-batching`.
- KEEP updates deterministic: fixed-step-driven and seeded. SEE `determinism`.
- USE GPU-side attribute updates (compute/instanced buffers) when particle counts are large.
- BUDGET particle counts; clamp emissions under frame-time pressure. SEE `performance-budget`.
