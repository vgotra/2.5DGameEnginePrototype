# AGENTS.md

Windows-first (at current moment) .NET 10 isometric game engine prototype. Vulkan is the default/main renderer with a GDI reference fallback. Before starting work, read `docs/README.md` (docs index), `.agents/context/FilesMap.md` (repo map), the relevant topic docs, and `.agents/context/*` (CurrentState, NextSteps, Decisions, KnownIssues) and update `.agents/context/` plus `RELEASE_NOTES.md` after each milestone — those files are the resumable-state convention.

## Commands
- Build (must be 0 warnings): `dotnet build Engine.sln --nologo` — `TreatWarningsAsErrors` is on.
- Brief tests are a plain console app, NOT a test framework: `dotnet run --project tests\Engine.Tests\Engine.Tests.csproj`. Success = prints `Smoke tests passed`. Do not use `dotnet test`.
- Run the sample (opens a window; needs the Vulkan SDK installed): `dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --vulkan`
- Sample flags: `--vulkan` (default), `--gdi`, `--2d` (flat top-down), `--fullscreen`. `F11` toggles fullscreen; `Escape` exits.
- Requires .NET 10 SDK (`global.json` pins 10.0.100, prerelease allowed; projects are `net10.0` with `LangVersion preview`).

## Shaders — recompile after editing GLSL
- GLSL sources live in `assets/shaders/`; the compiled `.spv` files are committed and copied to the output `shaders/` dir by `Engine.Rendering.Vulkan.csproj`.
- After editing a `.glsl`, run `tools\CompileShaders.ps1` (needs `glslc` from the Vulkan SDK `Bin` on PATH) and commit the updated `.spv`.
- Do NOT negate Y in `shape.vert.glsl`: screen space is y-down and `gl_Position` stores `ndc.y` unchanged (Vulkan NDC). Flipping it silently breaks Vulkan/GDI parity.

## Architecture
- `src/` separates contracts from backends. `Engine.Platform`, `Engine.Rendering`, `Engine.Audio`, `Engine.Physics` are contract/interface projects (`IRenderer`, `IGameWindow`, `SpritePacket`). `Engine.Platform.Win32` and `Engine.Rendering.Vulkan` are the concrete backends. `Engine.Platform.Desktop.GamePlatform.CreateWindow` is the platform factory/seam. `Engine.Core` (clock/handles), `Engine.Mathematics` (isometric), `Engine.Threading` (jobs), `Engine.Ecs`.
- `samples/IsometricSandbox/Program.cs` is the executable vertical slice (top-level statements, two duplicated game loops: Vulkan + GDI); it talks only to platform contracts + `Engine.Platform.Desktop`, never to Win32 directly. `tests/Engine.Tests` references the sample project to exercise game code (`MovementSystem`, `RenderExtractionSystem`, camera).
- Both backends must stay visually identical (white/black diamonds in iso, boxes in `--2d`); changing `IRenderer` means updating both `VulkanRenderer` and the GDI `Win32TileRenderer`.

## Platforms
- Windows-first: Vulkan (default) + GDI fallback. Linux (X11/Wayland via SDL2) and macOS are planned, later — see `docs/LinuxSupportPlan.md`.
- Keep new OS-specific code behind `Engine.Platform.*` backends; never put platform P/Invoke in contracts or shared gameplay code.
- `VulkanRenderer` takes a platform-neutral `NativeWindowSurface`; new platform surfaces are added by selecting the right instance extension — never add Win32-only parameters.
- GDI is Windows-only by nature; on non-Windows platforms Vulkan is the only renderer.

## Conventions that differ from .NET defaults
- No reflection, no LINQ, no managed allocation in runtime hot paths (structs, spans, preallocated buffers). See `docs/Conventions.md`.
- Central package management: add new packages to `Directory.Packages.props` (only `Vortice.Vulkan` 3.2.3 today), reference without a version.
- The Vulkan backend serializes the GPU with a per-frame staging upload + `vkQueueWaitIdle` (`BatchRenderer.cs`, `TextureUploader.cs`) — known and intentional for the MVP.
- `SpritePacket.Texture`/`Material` are ignored by the shape pipeline — texture sampling is a planned feature, not a bug.

## Principles
Develop code with **SOLID**, **KISS**, and **DRY**:
- Small, single-responsibility types and methods with clear names; prefer explicit ownership over hidden state.
- Keep it simple: the smallest solution that works; do not add speculative abstraction or generality.
- Don't repeat yourself: reuse existing code and contracts instead of duplicating logic; when a pattern appears twice, extract and share it.
- Code must be **easy to understand, refactor, and support**: follow existing project patterns and conventions, keep hot-path constraints (see Conventions), and keep OS-specific code behind the platform seams (see Platforms).

## Current work in flight
- Next roadmap features (`docs/Roadmap.md`): frame pacing / clean shutdown, texture sampling, asset loading, ECS queries + system scheduling, profiling.
- Cross-platform seams (contracts, `GamePlatform` host, `NativeWindowSurface`) are in place; the Linux/SDL2 platform backend is a later milestone.
