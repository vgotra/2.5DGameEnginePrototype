# Release Notes

## 2026-08-02
- Fixed the `renderdoc` MCP server, which was failing to start (`server unavailable`). Root cause: `renderdoc-mcp` 0.2.7 declares `mcp>=1.0.0` but breaks on `mcp` 2.x (`mcp.server.fastmcp` was removed). The launch now pins `--with "mcp>=1.0,<2"` in `opencode.json` and `.agents/mcp.json`, and the startup timeout was raised to 120s for cold-start (first uvx run downloads Python 3.13 + the wheel). Also fixed the `vulkan` MCP server's `vulkan-site-index` resource, which failed with "Invalid URL" because its URI had no scheme (`tools/mcp/mcp-Vulkan/vulkan/src/index.ts` now registers `vulkan-site://vulkan-site-index`; rebuilt). Documented both in `docs/AgentTooling.md` / `docs/RenderDocSetup.md`.
- Diagnosed the Vulkan-vs-GDI smoothness gap (FIFO vsync + double buffering + single in-flight fence + per-frame staging upload/`vkQueueWaitIdle` before present → visible judder; GDI's immediate `BitBlt` does not) and added the planned fix as roadmap item 1 in `docs/FramePacingPlan.md` (Mailbox + triple buffering, per-swapchain-image fences, high-res dt, persistent dirty-gated buffers, clean shutdown). Docs-only; no code change.
- Replaced the Grounded Docs MCP server (`@arabold/docs-mcp-server`) with a Vulkan-focused set in `opencode.json` and `.agents/mcp.json` (kept in sync): `vulkan` (`gpx1000/mcp-Vulkan`, Vulkan registry lookup, built by the new `tools/Setup-McpServers.ps1` into gitignored `tools/mcp/`) and `renderdoc` (`uvx --python 3.13 renderdoc-mcp`, GPU frame-capture analysis with a bundled RenderDoc replay module).
- Added `docs/RenderDocSetup.md` (install RenderDoc, capture frames from the sample, analyze `.rdc` through opencode) and indexed it plus `docs/AgentTooling.md` under a new "Tooling" section in `docs/README.md`.
- Added `AGENTS.md` with build, shader, and convention guidance for AI agents.
- Consolidated AI-agent assets from `.codex/` into `.agents/` (resumable context, skills, MCP catalog); added `opencode.json` with Context7 MCP.
- Replaced the Context7 MCP server with the open-source Grounded Docs MCP server (`@arabold/docs-mcp-server`, MIT, local) in `opencode.json` and `.agents/mcp.json`, and simplified `.agents/context/FilesMap.md` to folder-level functionality descriptions.
- Updated agent-path references across docs and cleaned change-log/attribution noise from documentation.
- Added `docs/LinuxSupportPlan.md` (Linux/SDL2, later macOS).
- Added cross-platform platform seams: `Engine.Platform` contracts (`IGameWindow`/`IInputState` extended, `PlatformKind`, `NativeWindowSurface`), the `Engine.Platform.Desktop.GamePlatform` host, and per-OS Vulkan loader/surface selection in `VulkanRenderer`. Sample now talks only to contracts + host; Win32-only types stay in `Engine.Platform.Win32`.
- Documented the cross-platform direction (Linux later, macOS possible) in agent docs and corrected `Architecture.md`, `Components.md`, `RenderingDesign.md`, `GameEngineDesign.md`, `README.md`, and `Roadmap.md`.
- Reorganized documentation for low token usage: `docs/README.md` index, topic files split out of `docs/ImplementationNotes.md` (`Windowing.md`, `ShaderWorkflow.md`, `Conventions.md`), agent-tooling reference moved to `docs/AgentTooling.md`, and `.agents/context/FilesMap.md` added (auto-loaded via `opencode.json` instructions). Added AGENTS.md `## Principles` (SOLID, KISS, DRY).

## 2026-08-01
- Initial engine bootstrap: SDK/build policy, core entity/clock types, isometric math, ECS storage, worker-scheduler foundation, backend-neutral contracts, deterministic tile map, continuous movement with collision and jump, camera follow, Win32 window/input, and the GDI reference renderer.
- Added the batched Vulkan renderer (`Engine.Rendering.Vulkan`) as the default backend implementing the backend-neutral `IRenderer`: SPIR-V shape shaders, per-frame staging uploads, single indexed draw.
- Added visual parity between backends (white diamonds with black borders in iso, white boxes in `--2d`), fixing Vulkan NDC-Y orientation.
- Added borderless fullscreen switching (`F11` / `--fullscreen`) with Vulkan swapchain rebuild and drag-resize handling.
- Added flat `--2d` projection mode via `ShapeKind` in both backends.
- Added the centered 800x600 window and map-bounds camera centering in fullscreen.
- Added smoke tests (`tests/Engine.Tests` console app).
