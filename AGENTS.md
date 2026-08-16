# Repository Instructions

- Windows-first .NET 10 isometric 2.5D game-engine prototype.
- Vulkan is the only renderer; SDL3 owns windowing, input, and Vulkan surfaces.
- `Engine.Ecs.Sparse` is the canonical ECS. Frame order is explicit; parallel work is opt-in and evidence-driven.
- Read `.agents/context/ProjectConfig.md` when concrete project values are relevant. It is the only auto-loaded project context file. Do not treat `benchmarks/` as default context: inspect or run it only for performance-regression work.
- Use the smallest relevant retained skill under `.agents/skills/`; do not load every skill for every task.

## Architecture rules

- Keep gameplay and engine contracts renderer-neutral; keep Vulkan details inside `src/Engine.Rendering.Vulkan` and SDL details inside `src/Engine.Platform.SDL3`.
- Use value-type ECS components, stable entity IDs, deferred structural changes through `EntityCommands`, and deterministic fixed-step simulation.
- Avoid reflection, LINQ, and managed allocation in hot paths. Prefer explicit ownership, spans, handles, and small single-purpose types.
- Preserve isometric painter order, the shared `SpritePacket` contract, and the existing Vulkan-only renderer seam.

## Verification

- After code or shader changes, use `.agents/skills/build-and-verify/SKILL.md`; the smoke tests are a plain console app, so use `dotnet run`, never `dotnet test`.
- Automated `IsometricSandbox` runs must use a positive `--frames` limit and `--cap 0`.
- Renderer changes also require the retained swapchain, frame-loop, batching, and profiling guidance. GLSL/SPIR-V changes require `shader-workflow` and synchronized shader outputs.
- Documentation-only changes need stale-reference and `git diff --check` validation; do not run the full runtime verification loop unless behavior changed.

## Scope

- Keep solutions simple and avoid speculative abstractions.
- Preserve `benchmarks/` and its regression coverage, but do not make benchmark files part of routine repository reading.
