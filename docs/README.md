# Documentation Index

Windows-first .NET 10 isometric game engine (Vulkan renderer; Linux/macOS planned). This file is the entry point for all documentation — read only this index (with its links); open another file only when you really need its details for the task. The codebase structure map comes from the `mcp-repo-graph` MCP server, not a file.

Status tags: **Implemented** (describes shipped behavior), **Plan** (ordered future work), **Design target** (architectural intent, not yet built), **Reference** (authoritative description of the current system), **Convention** (repo rules to follow), **Optional verification** (not started).

## For AI agents — recommended read order

1. `../AGENTS.md` — agent instructions (build/test/run commands live in `RunningAndVerifying/`), shader workflow, architecture, conventions, principles, platforms.
2. `.agents/context/` — resumable milestone state (auto-loaded via `opencode.json`): `CurrentState.md`, `KnownIssues.md`. For the structural map, use the `mcp-repo-graph` MCP server.
3. This index (read only this file; follow its links only when a task really needs the details in a topic file).

## Building, running, and verifying

- [`RunningAndVerifying/`](RunningAndVerifying/) — **Reference**. Prerequisites, build, test, sample run (flags and controls), and the verify checklist.

## Design and architecture

- [`GameEngineDesign.md`](GameEngineDesign.md) — **Reference**. Goals, frame lifecycle, ownership/perf model, modules, non-goals.

## Shaders

- [`ShaderWorkflow.md`](ShaderWorkflow.md) — **Reference**. GLSL → glslc → committed `.spv`; when and how to recompile; NDC-Y rule.

## Platforms

- [`LinuxSupportPlan.md`](LinuxSupportPlan.md) — **Plan**. Linux (SDL2, later macOS) support plan; platform seams in place.

## Performance and process

- [`Conventions/`](Conventions/) — **Convention**. Index of coding, code-style, packaging, restriction, and command conventions.
- [`Roadmap.md`](Roadmap.md) — **Plan**. Milestones, ordered feature queue, platform milestones, deferred items.
- [`RecoveryAndContext.md`](RecoveryAndContext.md) — **Convention**. The resumable-state convention every milestone must follow.

## Tooling

- [`AgentTooling.md`](AgentTooling.md) — **Reference**. MCP servers used by AI agents: install (`tools/Setup-McpServers.ps1`), config in `opencode.json` / `.agents/mcp.json`, relative-path resolution, and per-server guardrails.
- [`RenderDocSetup.md`](RenderDocSetup.md) — **Reference**. **Read only when you really need it.** Install RenderDoc, capture frames from the sample, analyze `.rdc` via the `renderdoc` MCP server.
