# Rendering Design

Rendering is split into backend-neutral extraction and a Vortice.Vulkan implementation. The backend-neutral contracts live in `Engine.Rendering`; `Engine.Rendering.Vulkan` owns Vulkan objects and GPU lifetime.

## Backend-neutral contracts

- `IRenderer`: `BeginFrame(Vector2 viewport)`, `Submit(ReadOnlySpan<SpritePacket>)`, `EndFrame()`. Backend handles never leak into gameplay code.
- `SpritePacket(Position, Size, Color, Texture, Material, SortKey, Shape)` — 48 bytes. `Texture`/`Material` handles are 4 bytes each and are currently ignored by the shape path. `Shape` (`ShapeKind.Diamond`/`ShapeKind.Box`) selects the generated geometry.
- `ShapePacket` — 40 bytes.
- `ShapeVertex(Position, Color)` — 24 bytes (`Vector2` + `Vector4`, sequential layout).
- `GeneratedGeometry.AppendDiamond` emits a 6-vertex isometric diamond (two triangles) centered on a screen-space position; the batch renderer emits the same pattern for `ShapeKind.Box` using axis-aligned corners.

## Coordinate convention

`IsometricMath.WorldToScreen` produces y-down screen coordinates (y = 0 is the top of the window). The SPIR-V vertex shader converts those to NDC and stores `gl_Position` with `ndc.y` unchanged: Vulkan NDC y = -1 already maps to the top of the framebuffer, so no negation is applied. A previous revision negated Y, which mirrored the image; the negation was removed from `shape.vert.glsl` and verified in the deployed SPIR-V (no `OpFNegate`).

## Vulkan implementation

- Requires the latest Vulkan SDK installed (runtime components provide `vulkan-1.dll`; `glslc` from the SDK's `Bin` compiles the shaders).
- Shaders are GLSL compiled with glslc into SPIR-V under `assets/shaders/`; `Engine.Rendering.Vulkan.csproj` copies `*.spv` to `shaders/` in the output directory.
- `VulkanRenderer` implements `IRenderer`: swapchain, render pass, per-swapchain framebuffers, graphics pipeline, per-frame command buffers, and acquire/present synchronization.
- `BatchRenderer` accumulates submitted packets into `ShapeVertex`/index lists and uploads them once per frame through a staging buffer with `vkQueueWaitIdle`, then issues a single indexed draw.
- `VulkanPipeline` owns the graphics pipeline and layout; the viewport size is passed as an 8-byte push constant (`vec2`).
- `VulkanBuffer`, `DescriptorSetAllocator`, and `TextureUploader` provide buffer, descriptor, and texture infrastructure ahead of the texture path.

## White/black tile style

Both backends draw the tile map and the player as white diamonds with black borders:

- Vulkan overdraws a slightly larger black diamond (size + 2 x 2 px border) behind each white diamond. Row-major submission makes shared tile edges read as uniform black grid lines.
- GDI fills each diamond with a white brush and outlines it with an explicit 1px black pen.

In `--2d` mode the projection is cartesian instead of isometric and the same border technique is applied to axis-aligned squares (64x64 tiles, 40x40 player): `SpritePacket.Shape` is `ShapeKind.Box`, so Vulkan emits axis-aligned corner geometry and GDI draws `Rectangle` fills with the same black pen.

## Window resize and fullscreen

`Win32Window` reports its client size through `WM_SIZE`, which the sample polls each frame. Both loops detect a size change and call `camera.Resize`; the Vulkan loop also calls `VulkanRenderer.Resize`, which recreates the swapchain, image views, and framebuffers (the command pool, command buffers, semaphores, and fence are created once and survive). `F11` toggles borderless fullscreen via `Win32Window.SetFullscreen`, which switches the window style to `WS_POPUP` sized to the monitor bounds and restores the saved rect on exit. The GDI renderer adapts automatically through `GetClientRect`. The window opens centered on the primary screen at 800x600. `IsometricCamera.Follow` clamps the camera to the map bounds (iso clamps the `(x+y)`/`(x-y)` axes, flat mode clamps `x`/`y`), so the map is centered on screen when it fits the viewport (fullscreen) and follows the player when it is larger than the viewport (windowed).

## Current limitations

- One staging upload + `vkQueueWaitIdle` per frame serializes the graphics queue (correct, not optimal).
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline; texture rendering is pending.
- Tile border thickness differs slightly between backends (Vulkan ~2px, GDI 1px).
