# Windowing

How windows, fullscreen, and resize work. Vulkan-side swapchain rebuild is covered in [`RenderingDesign.md`](RenderingDesign.md).

## Window state

- `Win32Window` reports client size through `WM_SIZE` — the single source of truth for windowed drag-resize, `F11` toggling, and `--fullscreen` start. The sample polls `IGameWindow.Size` each frame and calls `camera.Resize` on change.
- The window opens centered on the primary screen at 800x600 (centering via `GetSystemMetrics`).
- Fullscreen is **borderless**, not exclusive mode: `SetFullscreen` switches the style to `WS_POPUP` sized to the monitor bounds (`GetWindowRect`/`SetWindowPos`/`GetMonitorInfo`) and restores the saved rect + style on exit. `F11` toggles it; `--fullscreen` starts in it.
- On resize/fullscreen the sample also calls `VulkanRenderer.Resize`, which recreates the swapchain, image views, and framebuffers; command pool, command buffers, semaphores, and fence are created once and survive.
