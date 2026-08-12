---
name: animations-2d
description: Applies 2D animation conventions: animation state as flat struct components, batched updates via SIMD and parallel jobs, GPU-side frame calculation, and rendering through the box/quad path with the shared sprite packet contract. Use when writing or reviewing 2D sprite animation code in a game engine. Do not use for 2.5D/isometric animations (see animations-2-5d).
---

# Animations (2D)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- STORE animation state (atlas ID, frame index, timer) as flat `struct` components.
- BATCH animation updates via SIMD and parallel jobs.
- SHIFT frame calculation logic to the GPU where possible (passing sprite indices/UV offsets via instanced quads or compute shaders).
- RENDER 2D animation frames through the box/quad path using the same sprite packet contract as isometric diamonds.
