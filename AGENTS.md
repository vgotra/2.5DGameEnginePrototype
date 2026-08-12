# AGENTS.md

- Windows-first (at current moment) .NET 10 isometric game engine prototype. 
- Vulkan is the renderer. 
- Before starting work, read `.agents/context/*` (CurrentState, KnownIssues, ProjectConfig, Roadmap, Implemented, CompletedMilestones) — these are auto-loaded into every session; `CurrentState` is the present snapshot, `Implemented` is the brief feature inventory, and `CompletedMilestones` is the milestone history.
- For the codebase structure map, use the `mcp-repo-graph` MCP server (`orient` first; then `find`/`impact`/`trace`/`read`). MCP and tooling details: `.agents/README.md`.
- For conventions and workflows, load the relevant skill from `.agents/skills/` (index + placeholder glossary: `.agents/skills/README.md`). Skills are placeholder-based; substitute concrete values from `.agents/context/ProjectConfig.md`.

## Verification
- After any code or shader change, run the `build-and-verify` skill (build → smoke tests → sample run → benchmark gate). Key rule: the brief tests are a plain console app, NOT a test framework — do not use `dotnet test`.

## Shaders
- After editing a GLSL shader, run the `shader-workflow` skill (incremental recompile via `glslc` at build; manual fallback `tools\CompileShaders.ps1`).

## Architecture
- Vulkan is the only renderer (white/black diamonds in iso, boxes in `--2d`); changing `IRenderer` means updating `VulkanRenderer`.

## Principles
Develop code with **SOLID**, **KISS**, and **DRY**:
- Small, single-responsibility types and methods with clear names; prefer explicit ownership over hidden state.
- Keep it simple: the smallest solution that works; do not add speculative abstraction or generality.
- Don't repeat yourself: reuse existing code and contracts instead of duplicating logic; when a pattern appears twice, extract and share it.
- Code must be **easy to understand, refactor, and support**: follow existing project patterns and conventions, keep hot-path constraints (see `hot-path-interop`, `coding-runtime`, `memory-spans` skills), and keep OS-specific code behind the platform seams (see `platform-neutrality` skill).

## Roadmap
- Next roadmap features: see the next actions in `.agents/context/Roadmap.md`. Shipped milestones live in `.agents/context/Implemented.md`.

## Mandatory Implementation Workflow

All multi-step implementation work MUST follow the repository workflow defined in: `./AI_WORKFLOW.md`

This includes:
- roadmap milestones;
- features;
- refactors;
- migrations;
- architectural changes;
- bug-fix plans containing multiple implementation steps;
- any implementation plan approved by the user.

Before modifying code for any such task:

1. Read `./AI_WORKFLOW.md`.
2. Read `./.ai_workflow_logs/in_progress.md`.
3. Read `./.ai_workflow_logs/current_milestone.md`.
4. Consult `./.ai_workflow_logs/completed_items.md` before repeating or reimplementing existing work.
5. Resume existing in-progress work unless it is blocked or the user explicitly changes priority.

The workflow files are authoritative execution state.

Do NOT:
- start multi-step implementation without initializing/updating the workflow state;
- mark work complete before verification;
- redo work already recorded as verified;
- postpone workflow-log updates until the end of the session.

Keep `./.ai_workflow_logs/` synchronized with actual repository state throughout implementation.
