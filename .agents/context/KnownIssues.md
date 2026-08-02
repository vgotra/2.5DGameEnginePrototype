# Known Issues

- The current JobSystem is a provisional queue-based scheduler and does not yet implement dependency graphs or work stealing.
- The batch renderer uploads geometry through a staging buffer every time the geometry changes (FNV dirty gate); when it does upload, the staging→device copies are recorded into the main command buffer, so no per-frame queue drain occurs. `vkQueueWaitIdle` remains only for resize, dispose, and rare buffer growth.
- Frame pacing is implemented (roadmap item 1): MAILBOX present (fallback FIFO), triple buffering, a 3-slot frame-in-flight pool with per-swapchain-image fences, high-res `Stopwatch` dt with an optional `--cap` frame cap, and clean ESC/window-close teardown with `ErrorOutOfDateKHR` auto-resize. Sim-to-present interpolation is a deferred optional follow-up (see `docs/FramePacingPlan.md`).
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline; texture rendering is pending.
- Tile borders are drawn by overdrawing a slightly larger black diamond behind each white tile (~2px).
