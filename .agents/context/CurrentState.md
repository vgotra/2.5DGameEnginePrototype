# Current State

## Status

The engine is a Windows-first .NET 10 prototype for 2D/2.5D isometric games. The MVP vertical slice is complete: SDK/build policy, core entity and clock types, isometric math, ECS storage, backend-neutral contracts (rendering/audio/physics/platform), a deterministic tile map, continuous movement with collision and jump, camera following, Win32 window/input, and the Vulkan render path.

Rendering is split between a backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`/`UploadTexture`) and a single Vulkan backend:

- **Vulkan (`Engine.Rendering.Vulkan`)** — swapchain, render pass, per-swapchain framebuffers, SPIR-V shape shaders (GLSL compiled with glslc, copied to `shaders/` in output), graphics pipeline, descriptor pool allocator, texture uploader, and a batch renderer that accumulates submitted packets and issues per-texture-range indexed draws.
- **Texture sampling (Milestone A slice)** — `ShapeVertex` carries `Uv` (attribute location 2); the shape pipeline binds descriptor set 0 as a combined image sampler and the fragment shader samples `texture × inColor`. `TextureUploader` seeds a 1×1 white texture at handle 0 (default = identity) and uploads with a per-texture filter (`TextureFilter.Linear`/`Nearest`). `SpritePacket.Texture` is honored by the batch renderer; `Material` is still ignored. Atlases, bindless, and per-frame uploads are out of scope.
- **Asset loading (PNG)** — `PngLoader.Load(IRenderer, path, filter)` (StbImageSharp) decodes `assets/textures/*.png` (copied to output `textures/`) at startup and returns a `TextureHandle`; missing/corrupt files log `[assets] ...` and fall back to procedural/colored art. See `docs/AssetWorkflow.md`.

The sample is now the **"Archer in the Forest" mini-game** (`samples/IsometricSandbox`): a 20×20 tile map (grass, river with bridges, forest, bonfire, wall border), an archer player who aims at the mouse cursor and shoots arrows (left click), and deer/rabbits that wander, flee the player, are killed by arrows (+score in the window title), and respawn after 6 s. Entities render as upright textured quads (bottom-center anchored) in both iso and `--2d`; the scene is depth-sorted with a stable counting sort by SortKey (the renderer draws in submission order). Bonfire flicker is a per-frame color/tint modulation.

Windowing (`Engine.Platform.Win32`): the window opens centered on the primary screen at 800x600. `IInputState` now exposes `MousePosition` (client px), `IsMouseDown`, `MousePressed`, and the `Restart` (R) key via `Win32Input` (polled `GetCursorPos`/`ScreenToClient` + `GetAsyncKeyState`). `IsometricCamera` gained `ScreenToWorld` (affine inverse of `ScreenTransform`) for mouse aiming.

Vulkan is the only renderer.

The platform layer is cross-platform-ready. See `docs/LinuxSupportPlan.md`.
