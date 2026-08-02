# .agents/ — AI Agent Assets

This folder is the single, vendor-neutral home for everything AI-coding-agent related in this repo: skills, resumable milestone state, and MCP configuration. It replaces the old `.codex/` folder. The agent-features reference lives in `docs/AgentTooling.md`.

Tool discovery rules that shape this layout:

- **Skills** are auto-discovered from `.agents/skills/*/SKILL.md` by opencode and OhMyPi (omp) with zero config. No registration needed.
- **Instructions** must live at the repo-root `AGENTS.md` — the cross-tool AGENTS.md standard (Linux Foundation) is read by opencode, omp, GitHub Copilot, Codex, and 20+ other tools. omp explicitly skips AGENTS.md files inside dot-prefixed directories, so a `.agents/AGENTS.md` would be invisible to it. Root `AGENTS.md` is therefore the canonical instruction file.
- **MCP** servers live in `opencode.json` (opencode reads only that), with this `mcp.json` as the neutral catalog. Keep them in sync.
- Config is loaded only at agent startup. After changing `opencode.json`, skills, or agents, restart the agent.

## Layout

```
.agents/
├── README.md         # this file
├── context/          # resumable milestone state (was .codex/context), auto-loaded via opencode.json
│   ├── CurrentState.md   ├── NextSteps.md   ├── Decisions.md   ├── KnownIssues.md   └── FilesMap.md
├── skills/           # SKILL.md packs, auto-discovered
│   └── engine-development/SKILL.md
└── mcp.json          # neutral MCP catalog (mirrored into opencode.json)
```

Root glue files: `AGENTS.md` (canonical instructions), `opencode.json` (opencode config).

## Repo convention: resumable state

Read `.agents/context/` (CurrentState, NextSteps, Decisions, KnownIssues, FilesMap) before starting work on a milestone, and update it after implementation + verification so the next session can resume without conversation history. Milestones and the feature queue are in `docs/Roadmap.md`; the documentation index is `docs/README.md`; the cross-platform (Linux later, macOS possible) plan is in `docs/LinuxSupportPlan.md`.
