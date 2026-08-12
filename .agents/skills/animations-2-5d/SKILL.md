---
name: animations-2-5d
description: Applies 2.5D isometric animation conventions: animation state as flat struct components, batched updates via SIMD and parallel jobs, and GPU-side frame calculation via instanced rendering or compute shaders. Use when writing or reviewing isometric/diamond sprite animation code in a game engine. Do not use for flat 2D animations (see animations-2d).
---

# Animations (2.5D Isometric)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- STORE animation state (atlas ID, frame index, timer) as flat `struct` components.
- BATCH animation updates via SIMD and parallel jobs.
- SHIFT frame calculation logic to the GPU where possible (passing sprite indices/UV offsets via instanced rendering or compute shaders).
