# Current State

## Status

Bootstrap and gameplay slice created: SDK/build policy, core entity and clock types, isometric math utility, initial worker scheduler, ECS storage, backend-neutral contracts, deterministic tile map, continuous player movement, collision resolution, camera following, Win32 window/input with WASD and arrow keys, Vortice Vulkan package integration, Win32 surface creation, physical-device selection, logical device, graphics queue, swapchain, and a full Vulkan draw path.

The Vulkan backend now implements the backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`). It owns a render pass, per-swapchain framebuffers, SPIR-V shape shaders (compiled with glslc, copied to `shaders/` in output), a graphics pipeline, descriptor pool allocator, texture uploader, batch renderer with per-frame staging uploads, and acquire/present synchronization. The sample's `--vulkan` mode runs the full isometric scene (tile map + jumping player) through `IRenderer`; the GDI `--gdi` path remains as a fallback. The Vulkan NDC-Y orientation defect was fixed by removing the Y negation in `shape.vert.glsl` (`gl_Position` now uses `ndc.y` directly), verified in the deployed SPIR-V (no `OpFNegate`). Both renderers now draw all tiles and the player as white diamonds with black borders (tiles: black border sprite + white fill sprite per cell; GDI: white brush + explicit black pen outline).

## Added this milestone (Vulkan `IRenderer` + visual parity)

New files:

- `src/Engine.Rendering.Vulkan/BatchRenderer.cs` — sprite/shape batching, per-frame staging uploads, single indexed draw.
- `src/Engine.Rendering.Vulkan/VulkanPipeline.cs` — graphics pipeline and layout, 8-byte viewport push constant.
- `src/Engine.Rendering.Vulkan/VulkanBuffer.cs`, `DescriptorSetAllocator.cs`, `TextureUploader.cs` — buffer/descriptor/texture infrastructure ahead of the texture path.
- `assets/shaders/shape.vert.spv`, `assets/shaders/shape.frag.spv` — SPIR-V compiled from GLSL with glslc.

Changed:

- `src/Engine.Rendering.Vulkan/VulkanRenderer.cs` — now implements `IRenderer`; runs the full isometric scene in `--vulkan`.
- `assets/shaders/shape.vert.glsl` — removed the NDC-Y negation (orientation fix).
- `samples/IsometricSandbox/Game/RenderExtractionSystem.cs`, `Game/Win32TileRenderer.cs`, `Program.cs` — white-diamond-with-black-border styling in both backends; player drawn as black border + white fill sprite.
- `src/Engine.Rendering.Vulkan/Engine.Rendering.Vulkan.csproj` — copies `assets/shaders/*.spv` to `shaders/` in the output directory.

Follow-up fixes: the player diamond now uses a 2:1 aspect (40x20) so its edges run parallel to the tile edges (previously 36x22 caused a ~5-degree visual tilt). `Win32Input` now takes the window handle and gates sampling on `GetForegroundWindow()`, so the game only responds to keys while its window is focused (typing WASD in another app no longer moves the player; state is cleared when unfocused to avoid stuck keys). Cleanup: removed unused code (`GameComponents.cs`, `RenderExtractionSystem.ExtractMap`, `TileMap.WorldToTile`, `VulkanBuffer.CreateUniformBuffer`/`CopyTo`, the `fps`/`DrawFps` GDI path, dead ECS lines in `Program.cs`), and the batch upload path is now zero-allocation with a reused copy command buffer (per-frame staging upload + `vkQueueWaitIdle` remains). The `--gdi` flag replaced `--window`.

## Verification

`dotnet build Engine.sln --nologo` passes with 0 warnings and 0 errors. `dotnet run --project tests/Engine.Tests` prints `Smoke tests passed`. `IsometricSandbox --vulkan` opens a window and runs the render loop continuously without exceptions. Deployed `shape.vert.spv` hash matches the asset, and `spirv-dis` confirms the position store uses `ndc.y` directly.

## Next three actions

1. Stable game loop and window lifecycle (resize handling, frame pacing, clean shutdown).
2. Texture sampling path: bind descriptor sets, texture-blended sprites, and texture atlas support.
3. Profiling counters and allocation measurements for the draw path.

## Risks

The MVP renderer does one staging-buffer upload + `vkQueueWaitIdle` per frame, which serializes the queue; this is correct but not performance-optimal. Resize and swapchain rebuild are not yet handled. The batch renderer draws shapes only; `SpritePacket` texture/material handles are currently ignored.
