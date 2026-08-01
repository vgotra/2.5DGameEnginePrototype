# Known Issues

- Build and package restore have not yet been run because the desktop shell runner currently fails to launch PowerShell.
- External package versions require validation against the installed .NET 10 SDK and Vulkan SDK.
- The current JobSystem is a provisional queue-based scheduler and does not yet implement dependency graphs or work stealing.
- The batch renderer uploads geometry via a staging buffer with `vkQueueWaitIdle` every frame, serializing the graphics queue; acceptable for the MVP, not for production.
- Swapchain is created once at a fixed 960x640 and is not rebuilt on window resize; `IRenderer` has no resize notification yet.
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline; texture rendering is pending.
- Tile borders are drawn by overdrawing a slightly larger black diamond behind each white tile (Vulkan, ~2px) and via a 1px black pen (GDI); the two border thicknesses are tuned independently, so they may differ slightly.
