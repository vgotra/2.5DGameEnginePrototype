# Shader Workflow

How shaders are authored, compiled, shipped, and loaded.

## Sources, build, and deployment

- GLSL sources live in `assets/shaders/`; compiled SPIR-V (`.spv`) is **committed** and copied to the output `shaders/` directory by `Engine.Rendering.Vulkan.csproj`.
- After editing a `.glsl`, run `tools\CompileShaders.ps1` (requires `glslc` from the Vulkan SDK `Bin` on `PATH`) and commit the updated `.spv`. Never hand-edit `.spv`.
- The renderer loads shaders from `AppContext.BaseDirectory/shaders` (`VulkanRenderer.ShaderPath`).

## Coordinate convention (do NOT negate Y)

- Screen space is y-down: `y = 0` is the top of the window (`IsometricMath.WorldToScreen`).
- The vertex shader converts screen coordinates to NDC and stores `gl_Position` with **`ndc.y` unchanged**: Vulkan NDC `y = -1` already maps to the top of the framebuffer, so no negation is applied.
- **Do NOT add a Y negation to `shape.vert.glsl`.** A previous negation mirrored the image and broke the coordinate convention; it was removed and verified in the deployed SPIR-V (no `OpFNegate`).
