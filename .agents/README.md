# .agents/ — AI Agent Assets

This folder is the single, vendor-neutral home for everything AI-coding-agent related in this repo: skills, resumable milestone state, and MCP configuration. It replaces the old `.codex/` folder. The agent-features reference lives in `docs/AgentTooling.md`.

Tool discovery rules that shape this layout:

- **Skills** are auto-discovered from `.agents/skills/*/SKILL.md` by opencode and OhMyPi (omp) with zero config. No registration needed.
- **Instructions** must live at the repo-root `AGENTS.md` — the cross-tool AGENTS.md standard (Linux Foundation) is read by opencode, omp, GitHub Copilot, Codex, and 20+ other tools. omp explicitly skips AGENTS.md files inside dot-prefixed directories, so a `.agents/AGENTS.md` would be invisible to it. Root `AGENTS.md` is therefore the canonical instruction file.
- **MCP** servers live in `opencode.json` (opencode reads only that), with this `mcp.json` as the neutral catalog. Keep them in sync (full reference: `docs/AgentTooling.md`). Current servers: `vulkan` (Vulkan registry lookup, built by `tools/Setup-McpServers.ps1` into gitignored `tools/mcp/`), `renderdoc` (`.rdc` capture analysis via `uvx`, see `docs/RenderDocSetup.md`), `mcp-repo-graph` (codebase structural graph via `uvx`, caches to `.ai/repo-graph/`), `csharp` (Roslyn code intelligence, cloned + built by `tools/Setup-McpServers.ps1` into gitignored `tools/mcp/csharp-language-server/`), and `dotnet` (.NET SDK operations via `dotnet dnx`). The `uvx`/Python servers pin `mcp>=1.0,<2` — the `mcp` 2.x line dropped `mcp.server.fastmcp`, so newer `mcp` breaks them.
- Config is loaded only at agent startup. After changing `opencode.json`, skills, or agents, restart the agent.

## Layout

```
.agents/
├── README.md         # this file
├── context/          # resumable milestone state (was .codex/context), auto-loaded via opencode.json
│   ├── CurrentState.md   └── KnownIssues.md
├── skills/           # SKILL.md packs, auto-discovered
│   └── engine-development/SKILL.md
└── mcp.json          # neutral MCP catalog (mirrored into opencode.json)
```

Root glue files: `AGENTS.md` (canonical instructions), `opencode.json` (opencode config).

## Repo convention: resumable state

Read `.agents/context/` (CurrentState, KnownIssues) before starting work on a milestone, and update it after implementation + verification so the next session can resume without conversation history. Use the `mcp-repo-graph` server for the structural map of the code. Milestones and the feature queue are in `docs/Roadmap.md`; the documentation index is `docs/README.md`.
