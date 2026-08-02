# Documentation Index

Windows-first .NET 10 isometric game engine (Vulkan renderer; Linux/macOS planned). This file is the entry point for all documentation — read the relevant topic file instead of the whole tree to keep token use low. The repo structure map is `.agents/context/FilesMap.md`.

## For AI agents — recommended read order

1. `../AGENTS.md` — build/test/sample commands, shader workflow, architecture, conventions, principles, platforms.
2. `.agents/context/` — resumable milestone state (auto-loaded via `opencode.json`): `CurrentState.md`, `NextSteps.md`, `Decisions.md`, `KnownIssues.md`, plus the `FilesMap.md`.
3. This index → open only the topic files relevant to the task.

## Design and architecture

- [`Architecture.md`](Architecture.md) — project dependency graph (sample → contracts → backends → core); planned projects marked.
- [`GameEngineDesign.md`](GameEngineDesign.md) — goals, frame lifecycle, ownership/perf model, modules, non-goals.
- [`Components.md`](Components.md) — engine component inventory and NuGet dependency policy.
- [`ECSAndJobsDesign.md`](ECSAndJobsDesign.md) — ECS (archetypes/chunks/queries) and job system design targets.
- [`AudioAndPhysicsDesign.md`](AudioAndPhysicsDesign.md) — audio/physics contract + adapter design.

## Rendering

- [`RenderingDesign.md`](RenderingDesign.md) — backend-neutral rendering contracts, coordinate convention, Vulkan implementation, white/black tile style, current limitations.
- [`FramePacingPlan.md`](FramePacingPlan.md) — diagnosis (Vulkan judder on camera pans) and the planned frame-pacing/clean-shutdown work for roadmap item 1.
- [`ShaderWorkflow.md`](ShaderWorkflow.md) — GLSL → glslc → committed `.spv`; when and how to recompile; NDC-Y rule.
- [`Windowing.md`](Windowing.md) — window lifecycle: `WM_SIZE`, borderless fullscreen, swapchain resize.

## Platforms

- [`LinuxSupportPlan.md`](LinuxSupportPlan.md) — Linux (SDL2, later macOS) support plan; platform seams in place.

## Performance and process

- [`PerformanceBudget.md`](PerformanceBudget.md) — performance targets and multithreading policy.
- [`LibraryImportVerificationPlan.md`](LibraryImportVerificationPlan.md) — optional verification tiers (AOT publish, dotnet-counters/trace, BenchmarkDotNet, in-engine telemetry) for roadmap item #14's `[LibraryImport]` migration.
- [`Conventions.md`](Conventions.md) — coding/perf rules that differ from .NET defaults, package/build policy, commands.
- [`Roadmap.md`](Roadmap.md) — milestones, ordered feature queue, platform milestones, deferred items.
- [`RecoveryAndContext.md`](RecoveryAndContext.md) — the resumable-state convention every milestone must follow.

## Tooling

- [`AgentTooling.md`](AgentTooling.md) — AGENTS.md/skills/MCP/agents/commands/plugins reference for the repo.
- [`RenderDocSetup.md`](RenderDocSetup.md) — install RenderDoc, capture frames from the sample, analyze `.rdc` via the `renderdoc` MCP server.

## History

- [`RELEASE_NOTES.md`](../RELEASE_NOTES.md) — dated change log.
