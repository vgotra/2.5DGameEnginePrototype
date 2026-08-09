# Agent Tooling (MCP servers) — Reference

How the AI-agent tooling in this repo is installed, configured, and used. MCP servers are registered in `opencode.json` — opencode reads MCP configuration only from there (`.agents/mcp.json` / `.mcp.json` are not read by opencode). Config is loaded only at agent startup; after changing `opencode.json`, restart opencode.

## Servers

| Name | Purpose | Launch | Install |
|------|---------|--------|---------|
| `vulkan` | Vulkan registry lookup | `node tools/mcp/mcp-Vulkan/vulkan/build/index.js` | `tools/Setup-McpServers.ps1` → gitignored `tools/mcp/` |
| `mcp-repo-graph` | Codebase structural graph | `uvx --python 3.13 --with "mcp>=1.0,<2" mcp-repo-graph --repo .` | uvx; caches to `.ai/repo-graph/` |

All paths in `opencode.json` are **relative to the workspace root** (the MCP process CWD). If a relative path ever misbehaves, fall back to an absolute path for that entry only.

## Getting started

1. Run `.\tools\Setup-McpServers.ps1` to ensure the gitignored local checkout (`mcp-Vulkan`) is cloned and built.
2. Restart opencode so the MCP config from `opencode.json` loads.
3. Verify wiring: the `vulkan` and `mcp-repo-graph` tools should be available.

## Agent skills

Reusable, just-in-time-loaded agent instructions live in `.agents/skills/<name>/SKILL.md` (opencode discovers them at startup). The `vulkan` MCP is advisory only — consult it when exact spec fields, extension dependencies, or VUIDs matter; otherwise rely on the `Vortice.Vulkan` binding plus the agent's knowledge.

| Skill | Trigger |
|------|---------|
| `engine-verify` | Post-change verification loop (build → smoke tests → sample run → benchmark gate). |
| `shader-compile` | Recompiling GLSL → SPIR-V after editing `assets/shaders/*.glsl`. |
| `vulkan-backend` | Work in `Engine.Rendering.Vulkan` (swapchain, pipeline, batch renderer, texture uploads). |
| `milestone-context` | Updating resumable state (`.agents/context/*`, `RELEASE_NOTES.md`, docs index) after a milestone. |
| `engine-conventions` | Writing or reviewing engine code against the repo's coding/hot-path/platform-neutrality rules. |

Adding a skill: create `.agents/skills/<name>/SKILL.md` with YAML frontmatter — `name` (must match the directory, lowercase-hyphenated) and a trigger-optimized `description` (max 1024 chars, include negative triggers). The docs remain the single source of truth; skills link to them instead of restating detail. Restart opencode to pick up new or edited skills.
