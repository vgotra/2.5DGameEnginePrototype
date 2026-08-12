# AGENTS.md

- Windows-first (at current moment) .NET 10 isometric game engine prototype. 
- Vulkan is the renderer. 
- Before starting work, read `.agents/context/*` (CurrentState, KnownIssues, ProjectConfig, Roadmap, Implemented) — these are auto-loaded into every session and are the resumable-state convention; keep updates brief (terse bullets only).
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

## Milestone / Plan Workflow
- `.ai_workflow_logs/` (local-only, git-ignored) tracks implementation of any multi-step plan — a roadmap milestone, a feature, a refactor, or a plan approved for implementation — for AI + user continuity; keep the files terse and human-readable.
- At plan/milestone start, fill `current_milestone.md` with the checklist (one concrete, verifiable item per line) and note the status/date.
- When starting an item, move it to `in_progress.md` and record context/decisions/blockers under `Notes`.
- When an item is complete and verified, move it to `completed_items.md` (newest on top) with a one-line verification note.
- Context-loss/recovery rule: on any new or resumed session, read `in_progress.md` first, then `current_milestone.md`; never redo anything already in `completed_items.md`.
