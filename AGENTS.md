# AGENTS.md

Windows-first (at current moment) .NET 10 isometric game engine prototype. Vulkan is the renderer. Before starting work, read `docs/README.md` (the docs index — read only this index file with its links; open topic files only when you really need their details for the task) and `.agents/context/*` (CurrentState, KnownIssues) and update `.agents/context/` after each milestone — those files are the resumable-state convention. For the codebase structure map, use the `mcp-repo-graph` MCP server (no FilesMap file). The repo root already contains `opencode.json` (opencode config: `instructions` auto-loads `.agents/context/*`; MCP registers the `vulkan`, `renderdoc`, and `mcp-repo-graph` servers — see `docs/AgentTooling.md`).

## Commands
- Build, test, run, flags, controls, and the verification checklist live in `docs/RunningAndVerifying/` — the single source of truth. Key rules: The brief tests are a plain console app, NOT a test framework — do not use `dotnet test`.

## Shaders — recompile after editing GLSL
- See `docs/ShaderWorkflow.md`.

## Architecture
- Vulkan is the only renderer (white/black diamonds in iso, boxes in `--2d`); changing `IRenderer` means updating `VulkanRenderer`.

## Conventions that differ from .NET defaults
- See `docs/Conventions/`.

## Principles
Develop code with **SOLID**, **KISS**, and **DRY**:
- Small, single-responsibility types and methods with clear names; prefer explicit ownership over hidden state.
- Keep it simple: the smallest solution that works; do not add speculative abstraction or generality.
- Don't repeat yourself: reuse existing code and contracts instead of duplicating logic; when a pattern appears twice, extract and share it.
- Code must be **easy to understand, refactor, and support**: follow existing project patterns and conventions, keep hot-path constraints (see Conventions), and keep OS-specific code behind the platform seams (see Platforms).

## Current work in flight
- Next roadmap features (`docs/Roadmap.md`).
