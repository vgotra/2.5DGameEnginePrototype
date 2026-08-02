# Known Issues

- The current JobSystem is a provisional queue-based scheduler and does not yet implement dependency graphs or work stealing.
- The batch renderer uploads geometry via a staging buffer with `vkQueueWaitIdle` every frame, serializing the graphics queue; acceptable for the MVP, not for production.
- Vulkan frame delivery is uneven: the swapchain uses FIFO vsync (`presentMode = modes[0]`) with double buffering and a single in-flight fence, and every frame runs a staging upload + `vkQueueWaitIdle` before present — timing jitter becomes visible judder on camera pans. Planned fix tracked as roadmap item 1 / `docs/FramePacingPlan.md`.
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline; texture rendering is pending.
- Tile borders are drawn by overdrawing a slightly larger black diamond behind each white tile (~2px).
