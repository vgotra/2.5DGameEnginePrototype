# 2D/2.5D Isometric Game Engine

Windows-first .NET 10 prototype using Vortice.Vulkan. The first target is a small Diablo-like isometric vertical slice with explicit ownership, data-oriented ECS, mandatory multithreading, and low-allocation frame execution.

See [docs/GameEngineDesign.md](docs/GameEngineDesign.md) for the architecture and [.codex/context/CurrentState.md](.codex/context/CurrentState.md) for resumable implementation state.

## Execute the MVP

Requirements: Windows and the .NET 10 SDK.

From the repository root:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --window
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

Run Vulkan initialization and one clear frame:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --vulkan
```

The current visible MVP uses the simple Win32 double-buffered renderer. Vulkan initialization and swapchain code are present, while the final textured/batched Vulkan game renderer remains a later step.

## What to implement next

The prioritized feature list is maintained in [docs/Roadmap.md](docs/Roadmap.md). The most valuable next step is the real Vulkan shape/sprite renderer, followed by asset loading, profiling, ECS queries, and dependency-aware background jobs.
