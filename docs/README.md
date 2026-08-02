# Documentation Index

Windows-first .NET 10 isometric game engine (Vulkan default, GDI reference fallback; Linux/macOS planned). This file is the entry point for all documentation — read the relevant topic file instead of the whole tree to keep token use low. The repo structure map is `.agents/context/FilesMap.md`.

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
- [`ShaderWorkflow.md`](ShaderWorkflow.md) — GLSL → glslc → committed `.spv`; when and how to recompile; NDC-Y rule.
- [`Windowing.md`](Windowing.md) — window lifecycle and GDI rendering: `WM_SIZE`, borderless fullscreen, dirty-gated repaint, backbuffer.

## Platforms

- [`LinuxSupportPlan.md`](LinuxSupportPlan.md) — Linux (SDL2, later macOS) support plan; platform seams in place.

## Performance and process

- [`PerformanceBudget.md`](PerformanceBudget.md) — performance targets and multithreading policy.
- [`Conventions.md`](Conventions.md) — coding/perf rules that differ from .NET defaults, package/build policy, commands.
- [`Roadmap.md`](Roadmap.md) — milestones, ordered feature queue, platform milestones, deferred items.
- [`RecoveryAndContext.md`](RecoveryAndContext.md) — the resumable-state convention every milestone must follow.

## History

- [`RELEASE_NOTES.md`](../RELEASE_NOTES.md) — dated change log.
