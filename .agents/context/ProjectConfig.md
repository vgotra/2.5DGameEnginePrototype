# Project Config

Concrete values for the placeholder-based skills (see `.agents/skills/README.md`). Auto-loaded every session. Skills stay generic; substitute these values when applying them.

## Identity
- `<ProjectName>` — Engine
- `<SolutionName>` — Engine (`Engine.slnx`)
- `<GraphicsApi>` — Vulkan
- `<PhysicsLib>` — JoltC (Jolt)
- `<ImageDecodeLib>` — stb_image
- `<ShaderCompiler>` — glslc
- `<ShaderSourcesDir>` — `assets\shaders`
- `<TickRate>` — 60 Hz fixed step (`GameClock.FixedStep = 1.0 / 60.0`)
- `<ClockType>` — `GameClock` (`Engine.Core`)
- `<JobSystemType>` — `JobSystem` (`Engine.Threading`)
- `<EntityId>` — `EntityId` (`Engine.Core`)
- `<CommandBuffer>` — `EntityCommands` (`Engine.Ecs.Sparse`)
- `<SpritePacket>` — `SpritePacket` (`Engine.Rendering`)
- `<PresentMode>` — MAILBOX (Vulkan swapchain present mode)

## Contracts
- `<NativeSurfaceContract>` — `NativeWindowSurface` (`Engine.Platform`)
- `<SurfaceFactoryContract>` — `IVulkanSurfaceFactory` (`Engine.Platform`; implemented by `SdlWindow`)

## Projects
- `<CoreProject>` — `src\Engine.Core\Engine.Core.csproj` (core types: `GameClock`, `FrameTimer`, `EntityId`, `DebugMetrics`)
- `<AppProject>` — `src\Engine.App\Engine.App.csproj` (game loop host, camera, ECS wiring; drives the frame)
- `<PlatformBackendProject>` — `src\Engine.Platform.Desktop\Engine.Platform.Desktop.csproj` (desktop entry: `GamePlatform.CreateWindow`; selects the SDL3 backend)
- `<Sdl3BackendProject>` — `src\Engine.Platform.SDL3\Engine.Platform.SDL3.csproj` (SDL3 P/Invoke via `ppy.SDL3-CS` + window/pump)
- `<RendererProject>` — `src\Engine.Rendering.Vulkan\Engine.Rendering.Vulkan.csproj` (Vulkan is the only renderer)
- `<SampleProject>` — `samples\IsometricSandbox\IsometricSandbox.csproj`
- `<TestProject>` — `tests\Engine.Tests\Engine.Tests.csproj` (plain console app, NOT a test framework — never `dotnet test`)
- `<BenchmarkProject>` — `benchmarks\Engine.Benchmark\Engine.Benchmark.csproj` (Release-only, no GPU)

## Prerequisites
- Windows (current platform; Linux/macOS planned; Android/iOS need their workloads + a Mac for iOS builds)
- .NET 10 SDK (`global.json` pins 10.0.100; projects `net10.0`, `LangVersion preview`)
- Vulkan SDK (`vulkan-1.dll` + `glslc` for the build-time shader compile)

## Sample flags / controls
- Flags: `--2d` (flat top-down view), `--fullscreen` (borderless start), `--cap <fps>` (frame-rate cap), `--metrics` (rolling table every 120 frames), `--parallel` (force supported parallel extraction paths; live Vulkan batch command recording remains serial after the rendering audit), `--simulation` (128×128 map + 100k critter ECS stress test), `--arpg` (realistic ARPG workload), and `--frames <n>` (bounded run for verification).
- Controls: `WASD`/arrows move, mouse aim, left-click shoot, `Space` jump, `R` restart, `F11` fullscreen toggle, `Escape` exit. Score is shown in the window title.

## Benchmark gate
- Run: `dotnet run -c Release --project <BenchmarkProject> -- --compare baseline`
- Verdicts: time WARN +15% / FAIL +30%; allocations FAIL over tolerance (default 0.5 B) or any gen0. Steady-state target 0 B/op, 0 collections. Same-machine only. Exit code 1 on FAIL.
- `<ResultsDir>` — `benchmarks\results` (`last.json` every run, gitignored; `baseline.json` on `--save`, committed).

## Assets
- Source: optional PNGs in `assets\textures\` copied to output `textures\` (`PreserveNewest`); each missing file is logged `[assets] missing ...` and falls back to procedural/colored art.
- Texture names (sample): `player`, `deer`, `rabbit`, `grass`, `water`, `tree`, `bonfire`, `wall`.
- Placeholder generator: `tools\GeneratePlaceholderTextures.ps1` (skip existing unless `-Force`).

## Shaders
- Sources: `assets\shaders\*.glsl`; compiled to `.spv` automatically at build (incremental, `glslc`); manual fallback `<ManualShaderCompileScript>`; gate with `<ShaderRequiredProperty>`.
- `<ManualShaderCompileScript>` — `tools\CompileShaders.ps1`
- `<ShaderRequiredProperty>` — `/p:ShadersRequired=true`
