---
name: lighting-2d
description: Applies 2D/2.5D lighting conventions: light sources, normal maps, and lighting passes that integrate with the sprite batch and shader effects. Use when writing or reviewing lighting, normal-map, or light-related shader code in a 2D/2.5D engine. Do not use for general shader editing (see shader-effects) or post-processing (see shader-effects).
---

# 2D / 2.5D Lighting

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- MODEL light sources as data (position, radius, color, intensity), not per-sprite code.
- USE normal maps to add depth to 2.5D sprites; keep maps in atlases/bindless. SEE `assets-io`.
- COMPUTE lighting in a batch or render-target pass, not per draw call. SEE `rendering-batching`.
- PASS light parameters via uniform buffers/instance data. SEE `shader-effects`.
- KEEP lighting deterministic and fixed-step-independent; it is presentation. SEE `game-loop-frame`.
- BUDGET light counts and radii on mobile. SEE `performance-budget`.
