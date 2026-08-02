# Known Issues

- The current JobSystem is a provisional queue-based scheduler and does not yet implement dependency graphs or work stealing.
- The batch renderer uploads geometry via a staging buffer with `vkQueueWaitIdle` every frame, serializing the graphics queue; acceptable for the MVP, not for production.
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline; texture rendering is pending.
- Tile borders are drawn by overdrawing a slightly larger black diamond behind each white tile (Vulkan, ~2px) and via a 1px black pen (GDI); the two border thicknesses are tuned independently, so they may differ slightly.
