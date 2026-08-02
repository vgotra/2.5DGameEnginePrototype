# Components and Dependencies

## Engine components

| Component | Purpose |
|---|---|
| `Engine.Core` | Entity IDs, timing, debug utilities, and shared low-level types. |
| `Engine.Mathematics` | Isometric world/screen conversion and vector math helpers. |
| `Engine.Ecs` | Generational entities and unmanaged component storage. |
| `Engine.Threading` | Worker-thread job scheduling foundation. |
| `Engine.Platform` | Backend-neutral window and input contracts (`IGameWindow`, `IInputState`), plus `PlatformKind` and `NativeWindowSurface`. |
| `Engine.Platform.Win32` | Windows window creation, keyboard input, native close handling, and Win32 interop. |
| `Engine.Platform.Desktop` | Platform host: `GamePlatform.CreateWindow` selects the window/input backend for the current OS (Win32 today; Linux/macOS planned). |
| `Engine.Rendering` | Backend-neutral `IRenderer` contract, render packets, and generated geometry. |
| `Engine.Rendering.Vulkan` | Vulkan loader, instance, surface, device, queue, swapchain, command buffers, frame submission, and the batched shape renderer (pipeline, per-frame staging uploads, SPIR-V shaders). Consumes a `NativeWindowSurface`. |
| `Engine.Audio` | Backend-neutral audio device, clip, voice, and listener contracts. |
| `Engine.Physics` | Backend-neutral physics body, raycast, and world contracts. |
| `IsometricSandbox` | The playable MVP sample: tile map, movement, collision, jump, camera, and the Vulkan render path. |
| `Engine.Tests` | Fast executable checks for math, ECS, collision, camera, and geometry behavior. |

## NuGet dependencies

### `Vortice.Vulkan` 3.2.3

Used by `Engine.Rendering.Vulkan` for Vulkan types, function loading, instance/device creation, surfaces, swapchains, synchronization, command buffers, and presentation.

### Packages held for future subsystems

- `JoltPhysicsSharp` — physics contracts exist, but Jolt is not integrated yet.

## MVP components

- `TileMap` — compact tile storage, walkability, tile centers, and occupancy checks.
- `WorldPosition` — continuous player world position.
- `Movement` — velocity and movement speed data contract.
- `TileCollider` — player collision radius.
- `SpriteVisual` — visual color, size, and sort data contract.
- `PlayerTag` — identifies the player entity.
- `IsometricCamera` — follows the player and converts world coordinates to screen coordinates.
- `VulkanRenderer` — Vulkan `IRenderer` implementation: swapchain, render pass, framebuffers, command buffers, synchronization.
- `BatchRenderer` — converts `SpritePacket`s into batched diamond geometry with per-frame staging uploads and one indexed draw.
- shape shaders (`shape.vert.glsl`/`shape.frag.glsl`) — SPIR-V compiled with glslc; the vertex shader uses the NDC Y convention directly (no negation).
