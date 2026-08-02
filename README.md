# 2D/2.5D Isometric Game Engine

Windows-first (at current moment) .NET 10 prototype using Vortice.Vulkan. The first target is a small Diablo-like isometric vertical slice with explicit ownership, data-oriented ECS, mandatory multithreading, and low-allocation frame execution.

See [docs/GameEngineDesign.md](docs/GameEngineDesign.md) for the architecture and [.agents/context/CurrentState.md](.agents/context/CurrentState.md) for resumable implementation state.

Platforms: Windows is supported today (Vulkan renderer). Linux (X11/Wayland via SDL2) and macOS are planned — see [docs/LinuxSupportPlan.md](docs/LinuxSupportPlan.md). The docs index is [docs/README.md](docs/README.md).

## How to run application

Requirements: Windows, the .NET 10 SDK, and the latest [Vulkan SDK](https://vulkan.lunarg.com/sdk/home) (installed with the default runtime components so `vulkan-1.dll` is available; `glslc` from the SDK's `Bin` is used to compile the shaders).

From the repository root:

Run flags:

- `--2d` — top-down 2D mode (flat white squares with black borders) instead of the isometric diamond view.
- `--fullscreen` — start the window in borderless fullscreen.

Examples:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --2d
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

Run the sample, which draws the full isometric scene through the batched Vulkan `IRenderer` implementation:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj
```

The sample renders an isometric tile map and a jumping player drawn as white diamonds with black borders through the batched `IRenderer` path (SPIR-V shape shaders, per-frame staging uploads, a single indexed draw). The window opens centered at 800x600. `F11` toggles borderless fullscreen (the swapchain is rebuilt on the size change), and window drag-resizing is handled the same way. The camera clamps to the map bounds and centers the map on screen when it fits the viewport (fullscreen); in windowed mode it follows the player. `--2d` switches the projection to a flat top-down grid of white squares with black borders using the same `IRenderer` submission. Texture sampling and frame pacing remain later steps.

## What to implement next

The prioritized feature list is maintained in [docs/Roadmap.md](docs/Roadmap.md). The most valuable next steps are the texture sampling path (bind descriptor sets, texture-blended sprites, atlas support), followed by asset loading, profiling, ECS queries, and dependency-aware background jobs.

## Release notes

See [RELEASE_NOTES.md](RELEASE_NOTES.md) for the change history.
