# 2D/2.5D Isometric Game Engine

Windows-first .NET 10 prototype using Vortice.Vulkan. The first target is a small Diablo-like isometric vertical slice with explicit ownership, data-oriented ECS, mandatory multithreading, and low-allocation frame execution.

See [docs/GameEngineDesign.md](docs/GameEngineDesign.md) for the architecture and [.codex/context/CurrentState.md](.codex/context/CurrentState.md) for resumable implementation state.

## Execute the MVP

Requirements: Windows, the .NET 10 SDK, and the latest [Vulkan SDK](https://vulkan.lunarg.com/sdk/home) (installed with the default runtime components so `vulkan-1.dll` is available; `glslc` from the SDK's `Bin` is used to compile the shaders).

From the repository root:

Run flags:

- `--gdi` — GDI reference/fallback renderer.
- `--vulkan` — batched Vulkan `IRenderer` backend (default when no backend flag is given).
- `--2d` — top-down 2D mode (flat white squares with black borders) instead of the isometric diamond view. Combines with a backend flag; `--2d` alone uses Vulkan.
- `--fullscreen` — start the window in borderless fullscreen.

Examples:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --vulkan --2d
```

Controls:

- `WASD` or arrow keys: move.
- `Space`: jump two tiles in the last movement direction.
- `F11`: toggle borderless fullscreen.
- `Escape`: close the game.

Build the complete solution:

```powershell
dotnet build Engine.sln
```

Run the smoke tests:

```powershell
dotnet run --project tests\Engine.Tests\Engine.Tests.csproj
```

Run the Vulkan renderer, which draws the full isometric scene through the batched `IRenderer` implementation:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --vulkan
```

Both backends render the same scene: an isometric tile map and a jumping player drawn as white diamonds with black borders. `--vulkan` runs through the batched `IRenderer` path (SPIR-V shape shaders, per-frame staging uploads, a single indexed draw) and is the default backend; `--gdi` is the GDI reference/fallback renderer. The two windows match in orientation and layout. The window opens centered at 800x600. `F11` toggles borderless fullscreen (Vulkan rebuilds its swapchain on the size change), and window drag-resizing is handled the same way. The camera clamps to the map bounds and centers the map on screen when it fits the viewport (fullscreen); in windowed mode it follows the player. `--2d` switches the projection to a flat top-down grid of white squares with black borders using the same `IRenderer` submission. Texture sampling and frame pacing remain later steps.

## What to implement next

The prioritized feature list is maintained in [docs/Roadmap.md](docs/Roadmap.md). The most valuable next steps are the texture sampling path (bind descriptor sets, texture-blended sprites, atlas support), followed by asset loading, profiling, ECS queries, and dependency-aware background jobs.

## Development history

The initial Win32 platform work and the GDI reference renderer were authored with Codex. The Vulkan renderer (`Engine.Rendering.Vulkan`) and the fullscreen, `--2d`, and windowing milestones (including the Vulkan default, centered 800x600 window, and map centering in fullscreen) were authored with OpenCode.
