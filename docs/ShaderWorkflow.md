# Shader Workflow

How shaders are authored, compiled, shipped, and loaded.

## Sources, build, and deployment

- GLSL sources live in `assets/shaders/`; compiled SPIR-V (`.spv`) is **committed** and copied to the output `shaders/` directory by `Engine.Rendering.Vulkan.csproj`.
- Shaders are compiled at **build time, cross-platform**: `Engine.Rendering.Vulkan.csproj` imports `ShaderCompile.targets`, which runs an incremental `CompileShaders` target before `Build`. Any `.glsl` newer than its committed `.spv` is recompiled with `glslc` (SPIR-V output is deterministic — a recompile of unchanged sources is byte-identical). No-op builds skip the work.
- `glslc` is located automatically: `/p:GlslcPath=<path>` override → `$(VULKAN_SDK)\Bin\glslc.exe` (Windows) → `$(VULKAN_SDK)/bin/glslc` (Unix) → `/usr/bin/glslc` → `/opt/homebrew/bin/glslc` → `PATH` (`where` / `command -v`).
- If `glslc` is not found, the build logs `[shaders] glslc not found; skipping shader recompilation (committed .spv will be used).` and falls back to the committed `.spv`; pass `/p:ShadersRequired=true` to turn that into a hard error (useful in CI).
- Workflow after editing a `.glsl`: just rebuild (`dotnet build Engine.slnx --nologo`) and commit both the `.glsl` and the regenerated `.spv`. Never hand-edit `.spv`.
- `tools\CompileShaders.ps1` remains available as a manual fallback (requires `glslc` from the Vulkan SDK `Bin` on `PATH`).
- The renderer loads shaders from `AppContext.BaseDirectory/shaders` (`VulkanRenderer.ShaderPath`).

## Coordinate convention (do NOT negate Y)

- Screen space is y-down: `y = 0` is the top of the window (`IsometricMath.WorldToScreen`).
- The vertex shader converts screen coordinates to NDC and stores `gl_Position` with **`ndc.y` unchanged**: Vulkan NDC `y = -1` already maps to the top of the framebuffer, so no negation is applied.
- **Do NOT add a Y negation to `shape.vert.glsl`.** A previous negation mirrored the image and broke the coordinate convention; it was removed and verified in the deployed SPIR-V (no `OpFNegate`).
