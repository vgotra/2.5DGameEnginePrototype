# Release Notes

## 2026-08-02
- Added `AGENTS.md` with build, shader, and convention guidance for AI agents.
- Consolidated AI-agent assets from `.codex/` into `.agents/` (resumable context, skills, MCP catalog); added `opencode.json` with Context7 MCP.
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
