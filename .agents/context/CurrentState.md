# Current State

## Status

The engine is a Windows-first .NET 10 prototype for 2D/2.5D isometric games. The MVP vertical slice is complete: SDK/build policy, core entity and clock types, isometric math, ECS storage, backend-neutral contracts (rendering/audio/physics/platform), a deterministic tile map, continuous movement with collision and jump, camera following, SDL3 window/input, and the Vulkan render path.

Rendering is split between a backend-neutral `IRenderer` contract (`BeginFrame`/`Submit(SpritePacket)`/`EndFrame`/`UploadTexture`) and a single Vulkan backend:

- **Vulkan (`Engine.Rendering.Vulkan`)** — swapchain, render pass, per-swapchain framebuffers, SPIR-V shape shaders (GLSL compiled with glslc, copied to `shaders/` in output), graphics pipeline, descriptor pool allocator, texture uploader, and a batch renderer that accumulates submitted packets and issues per-texture-range indexed draws.
- **Texture sampling (Milestone A slice)** — `ShapeVertex` carries `Uv` (attribute location 2); the shape pipeline binds descriptor set 0 as a combined image sampler and the fragment shader samples `texture × inColor`. `TextureUploader` seeds a 1×1 white texture at handle 0 (default = identity) and uploads with a per-texture filter (`TextureFilter.Linear`/`Nearest`). `SpritePacket.Texture` is honored by the batch renderer; `Material` is still ignored. Atlases, bindless, and per-frame uploads are out of scope.
- **Asset loading (PNG)** — `PngLoader.Load(IRenderer, path, filter)` (StbImageSharp) decodes `assets/textures/*.png` (copied to output `textures/`) at startup and returns a `TextureHandle`; missing/corrupt files log `[assets] ...` and fall back to procedural/colored art. See `docs/AssetWorkflow.md`.

The sample is now the **"Archer in the Forest" mini-game** (`samples/IsometricSandbox`): a 20×20 tile map (grass, river with bridges, forest, bonfire, wall border), an archer player who aims at the mouse cursor and shoots arrows (left click), and exactly 10 deer/rabbits (no respawn — killed animals stay dead until the run is restarted with R) that wander, flee the player, and are killed by arrows (+score in the window title). Entities render as upright textured quads (bottom-center anchored) in both iso and `--2d`; the scene is depth-sorted with a stable counting sort by SortKey (the renderer draws in submission order). Bonfire flicker is a per-frame color/tint modulation.

Sample tunables (window title/size, animal count, player speed/radius, jump duration/height) live in `Game/SampleConfig.cs`, and `ArcherGame.MaxAnimals` (10) caps the population (the ctor clamps its `animalCount` argument). The sample is split into small named types: `Program.cs` is a thin entry point (`Options.Parse` → `GameSession.Run`); `GameSession` wires window/renderer/world and drives the frame loop; `Player` owns the archer's movement/jump/aim state; `SceneRenderer` owns the Vulkan render path and sprite buffers; `Options` parses `--2d`/`--cap`/`--fullscreen`/`--metrics`. Tile extraction is one shared loop (`RenderExtractionSystem.ExtractTiles`), with `ExtractMapSprites` (used by tests/benchmarks) forwarding to it with no texture handling.

Windowing/input (`Engine.Platform.SDL3`): the window opens centered on the primary screen at 800x600 via SDL3 (`ppy.SDL3-CS`; `SdlRuntime` refcounts `SDL_Init(SDL_INIT_VIDEO | SDL_INIT_EVENTS)`/`SDL_Quit`, `SdlWindow` owns the `SDL_Window*`, `SdlInput` polls `SDL_GetKeyboardState`/`SDL_GetMouseState`). `IInputState` exposes `MousePosition` (client px), `IsMouseDown`, `MousePressed`, and the `Restart` (R) key. `IsometricCamera` has `ScreenToWorld` (affine inverse of `ScreenTransform`) for mouse aiming.

Aiming reliability: `SdlWindow` latches a left-click `SDL_EVENT_MOUSE_BUTTON_DOWN` (event-driven, so quick clicks that land entirely between two polls are still captured) and `SdlInput.MousePressed` consumes that latch each `Update()`; the sample freshens the camera transform (`camera.Follow`) immediately before `ScreenToWorld` at click time so the aim target matches the player's current position even after frame hitches.

The Vulkan renderer is surface-factory driven: `IVulkanSurfaceFactory` (implemented by `SdlWindow`) supplies required instance extensions from `SDL_Vulkan_GetInstanceExtensions` and creates/destroys the surface via `SDL_Vulkan_CreateSurface`/`SDL_Vulkan_DestroySurface`, bridged through `VkInstance.Handle`/`new VkSurfaceKHR((ulong)handle)`.

Vulkan is the only renderer.

The platform layer is cross-platform-ready. See `docs/LinuxSupportPlan.md`.

## Codebase refactor (milestone)

Behavior-preserving refactor, whole repo (excluding `tools/mcp/`):

- **Rendering simplification** (`Engine.Rendering.Vulkan`): the large methods in `VulkanRenderer`/`BatchRenderer`/`TextureUploader`/`VulkanPipeline` were split into small named helpers; shared, single-responsibility helper types extracted and given their own files — `CameraPushConstants`, `FrameGeometryCache` (per-slot byte-snapshot dirty gate), `OneShotCommandBuffer` (immediate uploads), `VulkanImage` (layout transitions), `VulkanDebug` (object labels/names).
- **1 type = 1 file**, repo-wide, strict (enums and 1-line handles included): every contract bundle was split (`RenderContracts`, `AudioContracts`, `PhysicsContracts`, `PlatformContracts`, `InputContracts`), plus sample `CameraProjection`/`CameraSystem`/`Animal`/`TileMap` into `ICameraProjection`/`IsometricProjection`/`OrthographicProjection`/`ScreenTransform`/`IsometricCamera`, `AnimalSpecies`/`AnimalSystem`, `TileType`; tests `SmokeTestRunner` → `TestCase`/`TestAssert`; benchmarks `BenchResult`/`BenchmarkComparer`/`BenchRunner` → per-type files; Win32 interop structs `POINT`/`RECT`/`MONITORINFO`/`MSG` split from `Win32Types.cs`.
- Verified after refactor: `dotnet build Engine.slnx` 0 errors/0 warnings, smoke tests print `Smoke tests passed`, sample boots clean, benchmark comparison vs baseline = PASS 15 / WARN 0 / FAIL 0 (no new allocations).

## SDL3 platform migration (milestone)

Replaced the native Win32 windowing/input path with SDL3 (`ppy.SDL3-CS` pinned `2026.722.0`) and removed `Engine.Platform.Win32` entirely:

- **Contracts** (`Engine.Platform`): `IVulkanSurfaceFactory` (required instance extensions + `CreateSurface`/`DestroySurface` as `nint` handles) is carried on `NativeWindowSurface.SurfaceFactory`; `PlatformKind.Win32` renamed to `PlatformKind.Sdl3` (`Sdl3, X11, Wayland, MacOs`). `IGameWindow`/`IInputState`/`GameKey` are unchanged.
- **SDL3 backend** (`Engine.Platform.SDL3`): `SdlRuntime` (refcounted `SDL_Init`/`SDL_Quit`), `SdlWindow : IGameWindow, IVulkanSurfaceFactory` (Vulkan/resizable/high-DPI window; pumps QUIT/CLOSE_REQUESTED → ShouldClose, RESIZED → Size, left-button-down latch; fullscreen via `SDL_SetWindowFullscreen`), `SdlInput` (key bitmask edges from `SDL_GetKeyboardState` + mouse state + consumed click latch).
- **Renderer** (`Engine.Rendering.Vulkan`): `VulkanRenderer` no longer knows any OS surface struct — instance extensions come from the factory, the surface comes from `factory.CreateSurface(_instance.Handle)`, teardown via `factory.DestroySurface`. The `VK_KHR_win32_surface`/`VkWin32SurfaceCreateInfoKHR` branch is gone.
- **Wiring**: `Engine.slnx` + `Engine.Platform.Desktop.csproj` reference SDL3 instead of Win32; `GamePlatform.CreateWindow` is SDL3 on all OSes (no more `PlatformNotSupportedException`). `Engine.Platform.Win32` directory deleted.
- Verified: `dotnet build Engine.slnx` 0 errors/0 warnings, smoke tests print `Smoke tests passed`, sample boots clean (iso, `--2d`, `--cap 60`, `--fullscreen`), `SDL3.dll` win-x64 + `SDL3-CS.dll` present in sample output.
