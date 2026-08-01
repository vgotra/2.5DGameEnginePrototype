# Decisions

- Windows-first prototype with portability boundaries.
- Vortice.Vulkan is the rendering backend.
- ECS uses manual registration and archetype chunks.
- Mature external libraries are adapters, not public engine contracts.
- Shaders are compiled from GLSL source with glslc into SPIR-V and shipped under `assets/shaders/`; the Vulkan project copies them to `shaders/` in the output directory.
- `IRenderer` is the backend-neutral draw contract (`SpritePacket` submission); Vulkan maps sprites to batched shape geometry until the texture path lands.
- Vulkan NDC Y convention: screen coordinates are y-down (y = 0 at top), and the vertex shader stores `gl_Position` with `ndc.y` unchanged because Vulkan NDC y = -1 maps to the top of the framebuffer. Do not negate Y.
- Tile borders use overdraw rather than separate line geometry: the Vulkan batch draws a slightly larger black diamond behind each white tile; the GDI renderer outlines with a 1px black pen. The same technique (with `ShapeKind.Box`) produces the flat 64x64 grid in `--2d` mode.
- The sample retains the GDI `--gdi` path as a reference/fallback alongside the Vulkan `--vulkan` path so both backends can be compared; no backend flag defaults to Vulkan.
- Fullscreen is borderless (window style `WS_POPUP` sized to the monitor bounds) rather than exclusive-mode; the Vulkan swapchain is rebuilt on size change. `F11` toggles it; `--fullscreen` starts in it.
- `--2d` is a projection/geometry modifier, not a backend: it reuses the same `IRenderer` submission by switching the camera to a cartesian mapping and emitting `ShapeKind.Box` geometry. Tile cell size stays `TileWidth` per axis (64x64 squares) so the flat grid matches the iso world scale.
- The GDI renderer paints the client area outside `WM_PAINT` (dirty-gated). `Win32Window` suppresses `WM_ERASEBKGND` (returns 1) and raises a repaint flag on `WM_PAINT`; the renderer calls `ValidateRect` after each blit. This keeps dirty rendering correct across resizes/fullscreen instead of painting on-demand in `WM_PAINT`.
- The window opens centered on the primary screen at 800x600 (`Win32Window` centers via `GetSystemMetrics`).
- The camera clamps to the map bounds in screen space (`IsometricCamera.Follow`): when the map fits a viewport axis (typical in fullscreen) that axis is centered on screen, otherwise the camera follows the player within the map. The same clamp drives both backends, so the map is centered identically in Vulkan and GDI fullscreen.

