# Current State

## Status

The engine is a Windows-first .NET 10 prototype for 2D/2.5D isometric games. The MVP vertical slice is complete: SDK/build policy, core entity and clock types, isometric math, ECS storage, backend-neutral contracts (rendering/audio/physics/platform), a deterministic tile map, continuous movement with collision and jump, camera following, Win32 window/input, and the Vulkan render path.

Rendering is split between a backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`) and a single Vulkan backend that draws white diamonds with black borders in iso and white boxes in `--2d`:

- **Vulkan (`Engine.Rendering.Vulkan`)** — swapchain, render pass, per-swapchain framebuffers, SPIR-V shape shaders (GLSL compiled with glslc, copied to `shaders/` in output), graphics pipeline, descriptor pool allocator, texture uploader, and a batch renderer that accumulates submitted packets and issues one indexed draw per frame. 

Windowing (`Engine.Platform.Win32`): the window opens centered on the primary screen at 800x600.

Vulkan is the only renderer.

The platform layer is cross-platform-ready. See `docs/LinuxSupportPlan.md`.
