# Rendering Design

Rendering is split into backend-neutral extraction and a Vortice.Vulkan implementation. The backend-neutral contracts live in `Engine.Rendering`; `Engine.Rendering.Vulkan` owns Vulkan objects and GPU lifetime.

## Backend-neutral contracts

- `IRenderer`: `BeginFrame(Vector2 viewport)`, `Submit(ReadOnlySpan<SpritePacket>)`, `EndFrame()`. Backend handles never leak into gameplay code.
- `SpritePacket(Position, Size, Color, Texture, Material, SortKey)` — 44 bytes. `Texture`/`Material` handles are 4 bytes each and are currently ignored by the shape path.
- `ShapePacket` — 36 bytes.
- `ShapeVertex(Position, Color)` — 24 bytes (`Vector2` + `Vector4`, sequential layout).
- `GeneratedGeometry.AppendDiamond` emits a 6-vertex isometric diamond (two triangles) centered on a screen-space position.

## Coordinate convention

`IsometricMath.WorldToScreen` produces y-down screen coordinates (y = 0 is the top of the window). The SPIR-V vertex shader converts those to NDC and stores `gl_Position` with `ndc.y` unchanged: Vulkan NDC y = -1 already maps to the top of the framebuffer, so no negation is applied. A previous revision negated Y, which mirrored the image; the negation was removed from `shape.vert.glsl` and verified in the deployed SPIR-V (no `OpFNegate`).

## Vulkan implementation

- Shaders are GLSL compiled with glslc into SPIR-V under `assets/shaders/`; `Engine.Rendering.Vulkan.csproj` copies `*.spv` to `shaders/` in the output directory.
- `VulkanRenderer` implements `IRenderer`: swapchain, render pass, per-swapchain framebuffers, graphics pipeline, per-frame command buffers, and acquire/present synchronization.
- `BatchRenderer` accumulates submitted packets into `ShapeVertex`/index lists and uploads them once per frame through a staging buffer with `vkQueueWaitIdle`, then issues a single indexed draw.
- `VulkanPipeline` owns the graphics pipeline and layout; the viewport size is passed as an 8-byte push constant (`vec2`).
- `VulkanBuffer`, `DescriptorSetAllocator`, and `TextureUploader` provide buffer, descriptor, and texture infrastructure ahead of the texture path.

## White/black tile style

Both backends draw the tile map and the player as white diamonds with black borders:

- Vulkan overdraws a slightly larger black diamond (size + 2 x 2 px border) behind each white diamond. Row-major submission makes shared tile edges read as uniform black grid lines.
- GDI fills each diamond with a white brush and outlines it with an explicit 1px black pen.

## Current limitations

- One staging upload + `vkQueueWaitIdle` per frame serializes the graphics queue (correct, not optimal).
- Swapchain is created once at 960x640 and not rebuilt on resize; `IRenderer` has no resize notification yet.
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline; texture rendering is pending.
- Tile border thickness differs slightly between backends (Vulkan ~2px, GDI 1px).
