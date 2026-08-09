---
name: shader-compile
description: Recompiles GLSL shaders to SPIR-V after editing assets/shaders/*.glsl. Use when changing shader sources or debugging shader build/deploy issues. Do not use for renderer C# changes that do not touch shaders.
---

## What I do

1. After editing a `.glsl`, rebuild: `dotnet build Engine.slnx --nologo`. The incremental `CompileShaders` target recompiles any `.glsl` newer than its `.spv` via `glslc`; no-op builds skip the work.
2. Commit BOTH the `.glsl` and the regenerated `.spv`. Never hand-edit `.spv`.
3. Output is deterministic — a recompile of unchanged sources is byte-identical. To confirm a change actually recompiled, check the `.spv` bytes/timestamp changed after touching the `.glsl`.

## Coordinate rule — do NOT negate Y

- `shape.vert.glsl` must NOT apply a Y negation: Vulkan NDC `y=-1` already maps to the top of the framebuffer, and screen space is y-down. A previous negation mirrored the image and was removed. When inspecting deployed SPIR-V, verify no `OpFNegate` on the position Y.

## Details

- glslc resolution order, the missing-compiler fallback, and `/p:ShadersRequired=true` live in `docs/ShaderWorkflow.md`; `tools\CompileShaders.ps1` is the manual fallback.
