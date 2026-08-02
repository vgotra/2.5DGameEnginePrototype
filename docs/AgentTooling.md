# Agent Tooling Reference

Current reference for AI-agent features as used in this repo (opencode + OhMyPi). See `.agents/README.md` for the folder layout and `https://opencode.ai/docs/` (config, agents, skills, mcp-servers, rules, permissions, plugins) for authoritative behavior.

## AGENTS.md (instructions)

- Root `AGENTS.md` is the canonical instruction file all tools read at session start. Keep it short (~100 lines) and point into `docs/` and `.agents/` for detail.
- Nested `AGENTS.md` files work per-subproject in monorepos (nearest-file-wins); not needed here yet.
- Extra instruction files can be wired into opencode via `opencode.json` -> `instructions` (paths or globs, plus remote URLs).

## Skills (SKILL.md)

- A skill is a folder with an exact-`SKILL.md` file. Required YAML frontmatter: `name` (lowercase-hyphen-separated, 1-64 chars, matches folder name) and `description` (drives when the agent triggers it — state both what it does and when to use it). Optional: `license`, `compatibility`, `metadata`.
- Skills are loaded on-demand (progressive disclosure): the agent sees only the name + description until it loads the body. Front-load trigger keywords in the description.
- Discovery paths: `.opencode/skills/`, `.claude/skills/`, `.agents/skills/` (project) and the same under `~/.config/opencode`, `~/.claude`, `~/.agents` (global).
- Permissions: `permission.skill` with `allow` / `ask` / `deny`, wildcard patterns (`internal-*`), overridable per agent.

## MCP servers

- Local servers: `{ "type": "local", "command": ["exe", "arg"], "environment": {...}, "cwd": "..." }`.
- Remote servers: `{ "type": "remote", "url": "...", "headers": {...}, "oauth": {...} }`; opencode auto-handles OAuth (`opencode mcp auth <name>`).
- Config values support `{env:VAR}` and `{file:path}` interpolation — never commit secrets.
- Disable a whole server's tools with `tools: { "<name>_*": false }` and re-enable per agent.
- MCP servers add context tokens; enable only what you need.
- `vulkan` (`gpx1000/mcp-Vulkan`, local): queries the Vulkan registry (`vk.xml`) — tools `search-vulkan-docs` / `get-vulkan-topic`. Built from source by `tools/Setup-McpServers.ps1` into the gitignored `tools/mcp/`; `opencode.json` points at the built `vulkan/build/index.js`. Endorsed by the Khronos Vulkan Tutorial.
- `renderdoc` (`renderdoc-mcp` via `uvx --python 3.13`, local): GPU frame-capture analysis of `.rdc` files (draw calls, pipeline state, pixel history). The MCP package bundles its own RenderDoc replay module, so analysis needs no local RenderDoc install. Install RenderDoc and capture the sample — see `docs/RenderDocSetup.md`.

## Agents / subagents

- opencode built-ins: `build` + `plan` (primary), `general` + `explore` + `scout` (subagents).
- Custom agents: `opencode.json` -> `agent` or `.opencode/agents/<name>.md` (frontmatter: `mode` primary/subagent/all, `description`, `model`, `permission`, `temperature`, `steps`, `color`).
- Per-agent permission keys: `read, edit, glob, grep, list, bash, task, external_directory, lsp, skill` (accept glob maps) plus `todowrite, question, webfetch, websearch` (flat action only). Last matching rule wins.

## Commands and plugins

- Slash commands: `.opencode/commands/<name>.md` (body is the prompt; `$ARGUMENTS` / `$1`… placeholders).
- Plugins: `.opencode/plugins/*.ts` auto-discovered; `Plugin = (input) => Promise<Hooks>` with `event`, `config`, `tool.execute.before/after`, `permission.ask`, etc.

## Tool-specific behavior

| Tool | Instructions | Skills | MCP |
| --- | --- | --- | --- |
| opencode | root `AGENTS.md` + `opencode.json` `instructions` | `.agents/skills`, `.opencode/skills`, `.claude/skills` | `opencode.json` -> `mcp` |
| OhMyPi (omp) | root `AGENTS.md` only (skips dot-dirs) | `.agents/skills` (canonical OMP-native) | `.mcp.json` |
| GitHub Copilot / gh | root `AGENTS.md` | `.github/skills` | per-host config |
