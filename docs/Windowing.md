# Windowing and GDI Rendering

How windows, fullscreen, resize, and the GDI reference renderer work. Vulkan-side resize (swapchain rebuild) is covered in [`RenderingDesign.md`](RenderingDesign.md).

## Window state

- `Win32Window` reports client size through `WM_SIZE` — the single source of truth for windowed drag-resize, `F11` toggling, and `--fullscreen` start. Both sample loops poll `IGameWindow.Size` each frame and call `camera.Resize` on change.
- The window opens centered on the primary screen at 800x600 (centering via `GetSystemMetrics`).
- Fullscreen is **borderless**, not exclusive mode: `SetFullscreen` switches the style to `WS_POPUP` sized to the monitor bounds (`GetWindowRect`/`SetWindowPos`/`GetMonitorInfo`) and restores the saved rect + style on exit. `F11` toggles it; `--fullscreen` starts in it.
- On resize/fullscreen the Vulkan loop also calls `VulkanRenderer.Resize`, which recreates the swapchain, image views, and framebuffers; command pool, command buffers, semaphores, and fence are created once and survive.

## Dirty-gated GDI rendering

The GDI renderer paints the full client area **outside** `WM_PAINT` (dirty-gated). Three pieces keep it correct across resizes/fullscreen without a stale or wiped frame:

- `Win32Window` suppresses `WM_ERASEBKGND` (returns 1) so the system background erase can't wipe the last frame.
- `WM_PAINT` sets a repaint flag consumed via `IGameWindow.ConsumeRepaint()`, which the GDI loop folds into its `renderDirty` flag.
- `Win32TileRenderer.Draw` calls `ValidateRect` after each blit so a pending paint can't erase it.

## GDI backbuffer

`Win32TileRenderer` draws off-screen into a cached compatible DC + bitmap and copies once with `BitBlt` (direct window drawing flickers because Windows can erase the client area between draw operations). Brushes and the pen are cached; the backbuffer is recreated only when the client size changes.
