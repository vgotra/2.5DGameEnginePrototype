# Agent Tooling (MCP servers) — Reference

How the AI-agent tooling in this repo is installed, configured, and used. MCP servers are registered in `opencode.json` (opencode reads only that) and mirrored in `.agents/mcp.json` (the neutral catalog for other MCP clients) — keep the two in sync. Config is loaded only at agent startup; after changing `opencode.json`, restart opencode.

## Servers

| Name | Purpose | Launch | Install |
|------|---------|--------|---------|
| `vulkan` | Vulkan registry lookup | `node tools/mcp/mcp-Vulkan/vulkan/build/index.js` | `tools/Setup-McpServers.ps1` → gitignored `tools/mcp/` |
| `renderdoc` | RenderDoc `.rdc` capture analysis | `uvx --python 3.13 --with "mcp>=1.0,<2" renderdoc-mcp` | uvx (downloads on first call); see `docs/RenderDocSetup.md` |
| `mcp-repo-graph` | Codebase structural graph | `uvx --python 3.13 --with "mcp>=1.0,<2" mcp-repo-graph --repo .` | uvx; caches to `.ai/repo-graph/` |
| `csharp` | Roslyn code intelligence (navigation, type info, diagnostics, refactoring, NuGet exploration) | `dotnet run --no-build --project tools/mcp/csharp-language-server/src/CsharpMcp` | `tools/Setup-McpServers.ps1` → gitignored `tools/mcp/csharp-language-server/` |
| `dotnet` | Structured .NET SDK operations (build, project, package, solution, SDK/template metadata) | `dotnet dnx Community.Mcp.DotNet@1.* --yes` | dnx (downloads the package from NuGet on first call; requires .NET 10 SDK) |

All paths in `opencode.json` / `.agents/mcp.json` are **relative to the workspace root** (the MCP process CWD). If a relative path ever misbehaves, fall back to an absolute path for that entry only.

## csharp (Roslyn code intelligence)

- Loads the workspace from `Engine.slnx` at the repo root (solution-based discovery), so it sees all 14 engine projects. Because discovery is solution-based, the gitignored server checkout under `tools/mcp/csharp-language-server/` is **not** pulled into the workspace.
- Key tools: `get_definition`, `get_references` (read/write classified), `get_implementations`, `get_type_hierarchy`, `get_call_hierarchy`, `get_diagnostics` (no build needed), `get_rename` (preview by default), `get_code_actions`, `get_completions`, `get_outline`, `find`, plus `nuget_search` / `nuget_packages` / `nuget_explore`. `nuget_explore` reads the real public API + XML docs of cached packages (e.g. Vortice.Vulkan 3.2.3).
- Quality tools are enabled: `quality_hotspots` and `generate_iso5055_report`.
- Prefer the `csharp_*` tools over `grep` for C# symbol/type/reference questions.

### Install / rebuild

`tools/Setup-McpServers.ps1` clones `jgauffin/csharp-language-server` into `tools/mcp/` (depth 1, gitignored) and builds it. The build passes `-p:ManagePackageVersionsCentrally=false` because the repo-root `Directory.Packages.props` enables Central Package Management, which the server's project does not use — without the override, building anything under the repo root fails with NU1008. Rebuild the checkout with `.\tools\Setup-McpServers.ps1 -Rebuild` after updating it.

## dotnet (.NET SDK operations)

- Structured alternatives to raw `dotnet` CLI: `dotnet_project` (New/Restore/Build/Run/Publish/Clean/Pack), `dotnet_package` (NuGet add/remove/search/update), `dotnet_solution` (sln/slnx membership), `dotnet_sdk` (SDK/runtime/template/framework metadata), `dotnet_tool`, `dotnet_workload`, `dotnet_dev_certs`. Read-only `dotnet://` resources (`sdk-info`, `runtime-info`, `templates`, `frameworks`, `workspace`) answer environment questions without running commands.
- Launched as `dotnet dnx Community.Mcp.DotNet@1.* --yes` — on Windows `dnx` is a `.cmd` shim (`C:\Program Files\dotnet\dnx.cmd`, which is just `dotnet dnx`), and opencode spawns MCP commands directly without a shell, so the bare shim name would not resolve. The package downloads from NuGet on first use.

### Guardrails

- **Never** use the `dotnet_project` **Test** action — it runs `dotnet test`, and this repo's brief tests are a plain console app, not a test framework (see `docs/RunningAndVerifying/`). Run them via the documented console invocation instead.
- The `dotnet_project` **Run** action launches the engine sample (opens a window, needs the GPU/Vulkan). `New`, `Add`, `Remove`, and `Clean` actions mutate the repo / solution.

## Getting started

1. Run `.\tools\Setup-McpServers.ps1` to ensure the gitignored local checkouts (`mcp-Vulkan`, `csharp-language-server`) are cloned and built.
2. Restart opencode so the MCP config from `opencode.json` loads.
3. Verify wiring: `csharp_get_definition` / `csharp_nuget_explore` / `dotnet_project` / `dotnet_sdk` should be available; the `csharp` workspace should report 14 projects loaded from `Engine.slnx`.
