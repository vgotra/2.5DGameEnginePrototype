# Decisions

- Windows-first prototype with portability boundaries.
- Vortice.Vulkan is the rendering backend.
- ECS uses manual registration and archetype chunks.
- Mature external libraries are adapters, not public engine contracts.
- Shaders are compiled from GLSL source with glslc into SPIR-V and shipped under `assets/shaders/`; the Vulkan project copies them to `shaders/` in the output directory.
- `IRenderer` is the backend-neutral draw contract (`SpritePacket` submission); Vulkan maps sprites to batched shape geometry until the texture path lands.
- Vulkan NDC Y convention: screen coordinates are y-down (y = 0 at top), and the vertex shader stores `gl_Position` with `ndc.y` unchanged because Vulkan NDC y = -1 maps to the top of the framebuffer. Do not negate Y.
- Tile borders use overdraw rather than separate line geometry: the Vulkan batch draws a slightly larger black diamond behind each white tile; the GDI renderer outlines with a 1px black pen.
- The sample retains the GDI `--gdi` path as a reference/fallback alongside the Vulkan `--vulkan` path so both backends can be compared.

