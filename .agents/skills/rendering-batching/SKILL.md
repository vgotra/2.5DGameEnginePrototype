---
name: rendering-batching
description: Applies rendering batching conventions: grouping sprites/instances into few draw calls, minimizing pipeline and state binds, painter's-algorithm draw order, and static/reused vertex buffers. Use when writing or reviewing renderer, sprite, or batching code in a 2D/2.5D engine. Do not use for pure simulation, asset loading (see assets-io), or shader editing (see shader-workflow).
---

# Rendering Batching

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- BATCH sprites and instances into the fewest draw calls possible. Group by texture/atlas/bind group first, then by draw order.
- ENFORCE painter's-algorithm ordering for transparency: sort back-to-front once per frame, not per draw.
- MINIMIZE state changes: keep pipeline, descriptor set, and buffer binds stable across a batch.
- REUSE vertex/index buffers; grow once and keep steady-state allocation at zero. SEE `hot-path-interop` and `memory-spans`.
- USE instancing for repeated geometry (tiles, particles, projectiles) instead of per-object draw calls.
- KEEP GPU uploads and staging copies on the main thread; workers only fill result slots. SEE `job-system`.
- USE texture atlases and bindless textures to avoid texture binds. SEE `assets-io`.
