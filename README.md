# 2D/2.5D Isometric Game Engine

Windows-first .NET 10 prototype using Vortice.Vulkan. The first target is a small Diablo-like isometric vertical slice with explicit ownership, data-oriented ECS, mandatory multithreading, and low-allocation frame execution.

See [docs/GameEngineDesign.md](docs/GameEngineDesign.md) for the architecture and [.codex/context/CurrentState.md](.codex/context/CurrentState.md) for resumable implementation state.

## Execute the MVP

Requirements: Windows and the .NET 10 SDK.

From the repository root:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --gdi
```

Controls:

- `WASD` or arrow keys: move.
- `Space`: jump two tiles in the last movement direction.
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

Both backends render the same scene: an isometric tile map and a jumping player drawn as white diamonds with black borders. `--vulkan` runs through the batched `IRenderer` path (SPIR-V shape shaders, per-frame staging uploads, a single indexed draw); `--gdi` is the GDI reference/fallback renderer. The two windows match in orientation and layout. Texture sampling, swapchain resize handling, and frame pacing remain later steps.

## What to implement next

The prioritized feature list is maintained in [docs/Roadmap.md](docs/Roadmap.md). The most valuable next steps are the texture sampling path (bind descriptor sets, texture-blended sprites, atlas support), followed by asset loading, profiling, ECS queries, and dependency-aware background jobs.
