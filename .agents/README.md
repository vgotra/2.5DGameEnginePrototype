# Agent Tooling

How the AI-agent tooling in this repo is configured and used. All agent instructions live under `.agents/`.

## Layout

| Path | Purpose |
|---|---|
| `.agents/context/*.md` | Auto-loaded into every session's instructions (`opencode.json` → `instructions`). `CurrentState.md` is the present-state snapshot; `Implemented.md` is the brief shipped-feature inventory; `CompletedMilestones.md` is the authoritative milestone history; `KnownIssues.md`, `Roadmap.md`, and `ProjectConfig.md` provide active limitations, next actions, and concrete configuration. Keep snapshots and inventories terse. |
| `.agents/skills/` | Portable, placeholder-based skills. The local `.agents/skills/README.md` is the index and placeholder glossary; the `SKILL.md` files are self-contained sources of truth. |
| `.agents/README.md` | This file: agent-tooling reference. |
| `opencode.json` | opencode config: `instructions` auto-loads `.agents/context/*`; `mcp` registers the `mcp-repo-graph` server. Config is loaded only at startup — after editing it, restart opencode. |
| `.ai/repo-graph/` | Cache for the `mcp-repo-graph` structural index. |

## MCP servers

| Name | Purpose | Launch |
|---|---|---|
| `vulkan` | Vulkan registry lookup | `node tools/mcp/mcp-Vulkan/vulkan/build/index.js` | `tools/Setup-McpServers.ps1` → gitignored `tools/mcp/` |
| `mcp-repo-graph` | Codebase structural graph | `uvx --python 3.13 --with "mcp>=1.0,<2" mcp-repo-graph --repo .` | uvx; caches to `.ai/repo-graph/` |

### MCP usage policy

- `mcp-repo-graph` is the preferred structural-navigation tool: run `orient` first, then use `find`, `impact`, `trace`, or `read`.
- The `vulkan` server is optional and is useful for Vulkan registry/API lookup; it is not required for ordinary local renderer edits.
- If an MCP server is configured but its tools are not available in the current agent session, record that limitation and use local `rg`, file inspection, and build tooling as the fallback. Do not invent MCP tool names or install a plugin implicitly.
- Web or unrelated connector MCPs are not required for local engine/Vulkan work.

All paths in `opencode.json` are relative to the workspace root. Config is loaded once at startup — restart opencode after changing `opencode.json`, an agent, a skill, or a context file to pick it up.

## Getting started

1. Ensure MCP prerequisites (`uvx`); restart opencode so the MCP config loads.
2. Verify wiring: the `mcp-repo-graph` tools should be available; if unavailable, use the documented local-search fallback.
3. For structural questions use `mcp-repo-graph` before grepping when available; for skill usage load the smallest relevant set from `.agents/skills/`.

## Agent skills

Reusable, just-in-time-loaded instructions live in `.agents/skills/<name>/SKILL.md` (opencode discovers them at startup). The skills are **generic and portable**: they use `<...>` placeholders (e.g. `<ProjectName>`, `<SolutionName>`) instead of concrete project names, so the same skill set works in any project of the same type.

Adding a skill: create `.agents/skills/<name>/SKILL.md` with YAML frontmatter — `name` (must match the directory, lowercase-hyphenated) and a trigger-optimized `description` (max 1024 chars, include negative triggers). Restart opencode to pick up new or edited skills.
