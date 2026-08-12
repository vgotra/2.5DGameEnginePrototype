---
name: shader-effects
description: Applies shader effect conventions: post-processing, blend modes, uniform/UBO parameter passing, and per-object effects via instance data. Use when writing or reviewing shader-backed effects in a graphics project. Do not use for the shader compile/recompile workflow (see shader-workflow) or general batching (see rendering-batching).
---

# Shader Effects

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- PASS per-effect parameters via uniform buffers; NEVER bind uniforms per draw in a hot loop — use instance data instead. SEE `rendering-batching`.
- HANDLE blend modes and alpha explicitly per effect; prefer fewer blend-state switches per frame.
- USE render targets for post-processing (screen-space effects); manage their lifetime like any GPU resource.
- KEEP the shader source and compiled bytecode workflow from the shader compile skill. SEE `shader-workflow`.
- VERIFY coordinate-system assumptions (no unintended Y negation) when adding effects. SEE `shader-workflow`.
- ENSURE effects preserve the deterministic render order (painter's algorithm) for transparency.
