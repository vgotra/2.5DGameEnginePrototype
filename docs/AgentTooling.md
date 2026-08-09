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
