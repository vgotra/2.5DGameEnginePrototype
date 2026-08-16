---
name: shader-workflow
description: Recompiles shader sources to bytecode after editing them, commits both source and output, and verifies coordinate-system assumptions (e.g. no Y negation). Use when changing shader sources or debugging shader build/deploy issues in a graphics project. Do not use for renderer C# changes that do not touch shaders.
---

# Shader Workflow

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying (e.g. `<ShaderSourcesDir>`, `<ShaderCompiler>`).

## Sources, build, deployment
- GLSL sources live in `<ShaderSourcesDir>/`; compiled bytecode (`.spv`) is **committed** and copied to the output `shaders/` directory by the renderer project.
- Shaders are compiled at **build time, cross-platform**: the renderer project imports a compile targets file that runs an incremental compile target before `Build`. Any source newer than its committed `.spv` is recompiled with `<ShaderCompiler>`; output is deterministic — a recompile of unchanged sources is byte-identical, and no-op builds skip the work.

## Rules
1. After editing a shader source, rebuild the solution: `dotnet build <SolutionName>.slnx --nologo` recompiles incrementally.
2. Commit BOTH the shader source and the regenerated output. Never hand-edit the compiled bytecode.
3. LOCATE the compiler by probing known install paths in order: a property override (e.g. `/p:GlslcPath=<path>`) → the graphics SDK's env var + fallback bin paths (Windows then Unix) → common system bin paths → `PATH` (`where` / `command -v`). The SDK's toolchain provides it.
4. If the compiler is missing, FALL BACK to the committed bytecode (never ship stale-recompiled output) and log a clear message. Gate the fallback with a hard-require switch (`<ShaderRequiredProperty>`) for CI so missing tooling fails the build instead of silently shipping stale `.spv`.
5. KEEP a manual recompile fallback script (`<ManualShaderCompileScript>`) for when the incremental target is not run; it requires the compiler on `PATH`.
6. The renderer loads shaders from `AppContext.BaseDirectory/shaders` at runtime.
7. SEE `asset-pipeline` for the cooking/determinism rules.

## Coordinate rule — do NOT negate Y
- Screen space is y-down (`y = 0` is the top of the window; `WorldToScreen`).
- The vertex shader converts screen coordinates to NDC and stores position with the NDC Y **unchanged**: the graphics API NDC already maps `y = -1` to the top of the framebuffer, so no negation is applied.
- **Do NOT add a Y negation to the shape vertex shader.** A previous negation mirrored the image and broke the coordinate convention. When inspecting deployed bytecode, verify no negation on the position Y (e.g. no `OpFNegate`).
