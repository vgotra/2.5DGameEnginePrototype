---
name: vulkan-backend
description: Implements or debugs the Vulkan renderer backend (Engine.Rendering.Vulkan): swapchain, pipeline, descriptors, batch renderer, texture uploads, draw submission. Use when changing Vulkan code or the IRenderer contract. Do not use for SDL3 windowing, platform, ECS, or gameplay code.
---

## What I do

Vulkan is the ONLY renderer: `IRenderer` (`BeginFrame` / `Submit(SpritePacket)` / `EndFrame` / `UploadTexture`) is implemented solely by `VulkanRenderer`. Changing `IRenderer` means updating `VulkanRenderer`.

## Structure and patterns

- 1 type = 1 file, repo-wide. Prefer extracting small named helper types over large methods: `CameraPushConstants`, `FrameGeometryCache` (per-slot byte-snapshot dirty gate), `OneShotCommandBuffer` (immediate uploads), `VulkanImage` (layout transitions), `VulkanDebug` (object labels/names).
- Consult the `vulkan` MCP server when exact spec fields, extension dependencies, or VUIDs matter; otherwise rely on the `Vortice.Vulkan` binding plus your knowledge. The MCP is advisory, not a gate.

## Batch renderer

- Accumulates submitted `SpritePacket`s and issues per-texture-range indexed draws (one draw per distinct texture range — no sorting/merging yet, so interleaved textures multiply draw calls). Renders in submission order (the scene is depth-sorted by a stable counting sort on SortKey upstream).
- `SpritePacket.Texture` is honored via per-texture-range descriptor binds; `Material` is still ignored; only one descriptor layout (combined image sampler in set 0) exists.

## Texture uploads

- `TextureUploader` seeds a 1x1 white texture at handle 0 (identity) and uploads with a per-texture filter (Linear/Nearest). Staging→device copies are recorded into the main command buffer (no per-frame queue drain); `vkQueueWaitIdle` stays for resize, dispose, and rare buffer growth.
- `TextureUploader` owns its OWN one-shot command pool — never borrow the renderer's per-frame `VkCommandPool`, which `Resize()` destroys/recreates (a stale borrow crashes `vkAllocateCommandBuffers`).
- Texture uploads are startup-only; atlases, bindless, and per-frame uploads are out of scope.

## Hot-path and interop rules

- Zero heap allocations in submission/render loops; no reflection/LINQ; explicit ownership of native buffers; `[LibraryImport]`/blittable types per `docs/Conventions/VulkanInterop.md`, `HotPath.md`, and `MemorySpans.md`.
- No OS surface types: consume `NativeWindowSurface` + an `IVulkanSurfaceFactory` (implemented by `SdlWindow`); instance extensions come from the factory, the surface via `CreateSurface`/`DestroySurface` as `nint`.
