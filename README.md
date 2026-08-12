# 2D/2.5D Isometric Game Engine

Windows-first (at current moment) .NET 10 prototype using Vortice.Vulkan. The engine uses a simple sparse-set ECS with explicit frame scheduling, adaptive/targeted multithreading, and low-allocation frame execution.

See [.agents/context/CurrentState.md](.agents/context/CurrentState.md) for the architecture and resumable implementation state.

Platforms: Windows is supported today (Vulkan renderer). Linux (X11/Wayland via SDL3) and macOS (SDL3 + MoltenVK) are planned verification targets — see [.agents/context/CurrentState.md](.agents/context/CurrentState.md) and [.agents/context/Roadmap.md](.agents/context/Roadmap.md). Agent conventions and workflows live in the skills index at [.agents/skills/README.md](.agents/skills/README.md).

## How to run, verify, and test

Prerequisites, build, smoke tests, sample run (flags and controls), and the benchmark/verification loop are covered by the [`build-and-verify` skill](.agents/skills/build-and-verify/SKILL.md); concrete project names, commands, flags, and controls are in [.agents/context/ProjectConfig.md](.agents/context/ProjectConfig.md).
