# 2D/2.5D Isometric Game Engine

Windows-first (at current moment) .NET 10 prototype using Vortice.Vulkan. The first target is a small Diablo-like isometric vertical slice with explicit ownership, data-oriented ECS, mandatory multithreading, and low-allocation frame execution.

See [docs/GameEngineDesign.md](docs/GameEngineDesign.md) for the architecture and [.agents/context/CurrentState.md](.agents/context/CurrentState.md) for resumable implementation state.

Platforms: Windows is supported today (Vulkan renderer). Linux (X11/Wayland via SDL2) and macOS are planned — see [docs/LinuxSupportPlan.md](docs/LinuxSupportPlan.md). The docs index is [docs/README.md](docs/README.md).

## How to run, verify, and test

Prerequisites, build, smoke tests, sample run (flags and controls), and the verification checklist are the single source of truth in [docs/RunningAndVerifying/](docs/RunningAndVerifying/).
