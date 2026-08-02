# Files Map — repo structure at a glance

Use this to orient quickly; open a file only when your task needs its details. Docs index: `docs/README.md`.

```
2.5DGameEnginePrototype/
├── AGENTS.md                     # AI-agent instructions: commands, shaders, architecture, conventions, principles, platforms
├── opencode.json                 # opencode config: instructions (context + FilesMap), Context7 MCP
├── README.md                     # project readme (links docs/README.md, release notes, platform status)
├── RELEASE_NOTES.md              # dated change log (simple format)
├── LICENSE                       # license
├── Engine.sln                    # solution: 13 projects
├── Directory.Build.props         # net10.0, LangVersion preview, warnings-as-errors, nullable, unsafe
├── Directory.Packages.props      # central packages: Vortice.Vulkan 3.2.3
├── global.json                   # SDK 10.0.100 (prerelease allowed)
│
├── .agents/                      # AI-agent assets (skills, context, MCP catalog)
│   ├── README.md                 # folder guide (reference section lives in docs/AgentTooling.md)
│   ├── mcp.json                  # neutral MCP catalog: context7
│   ├── context/                  # resumable milestone state (auto-loaded via opencode.json instructions)
│   │   ├── CurrentState.md       # current implementation status + verification + risks
│   │   ├── NextSteps.md          # ordered follow-ups + roadmap status
│   │   ├── Decisions.md          # architecture/design decisions log
│   │   ├── KnownIssues.md        # known issues / intentional MVP tradeoffs
│   │   └── FilesMap.md           # this file
│   └── skills/
│       └── engine-development/SKILL.md   # engine-dev skill (platform-neutrality + conventions)
│
├── assets/shaders/               # GLSL sources + committed .spv (shape.vert/frag)
│
├── docs/                         # documentation (see docs/README.md index)
│
├── src/
│   ├── Engine.Core/              # GameClock, EntityId, DebugMetrics (no deps)
│   ├── Engine.Mathematics/       # IsometricMath (world<->screen conversion)
│   ├── Engine.Ecs/               # World, SparseSet, ComponentTypeId (deps: Core)
│   ├── Engine.Threading/         # JobSystem (deps: Core)
│   ├── Engine.Platform/          # contracts: IGameWindow, IInputState, GameKey, PlatformKind, NativeWindowSurface
│   ├── Engine.Platform.Win32/    # Win32Window, Win32Input (deps: Platform)
│   ├── Engine.Platform.Desktop/  # GamePlatform.CreateWindow host/factory (deps: Platform, Platform.Win32)
│   ├── Engine.Rendering/         # IRenderer, SpritePacket/ShapePacket/ShapeVertex, GeneratedGeometry (deps: Core)
│   ├── Engine.Rendering.Vulkan/  # Vulkan backend (deps: Platform, Rendering, Vortice.Vulkan)
│   │   ├── VulkanRenderer.cs     # instance/surface/swapchain/device; consumes NativeWindowSurface
│   │   ├── BatchRenderer.cs      # batched shape draw: staging upload + single indexed draw
│   │   ├── VulkanPipeline.cs / PipelineConfiguration.cs / ShapePipelineDescription.cs
│   │   ├── ShaderModuleLoader.cs # loads committed .spv from output shaders/
│   │   ├── DescriptorSetAllocator.cs / VulkanBuffer.cs / TextureUploader.cs  # texture-path infrastructure (unused yet)
│   ├── Engine.Audio/             # audio contracts only (no backend yet)
│   └── Engine.Physics/           # physics contracts only (no backend yet)
│
├── samples/IsometricSandbox/     # executable vertical slice (deps: Core, Math, Ecs, Platform.Desktop, Rendering.Vulkan)
│   ├── Program.cs                # top-level statements; two loops: Vulkan (default) + GDI fallback
│   └── Game/
│       ├── TileMap.cs            # deterministic tile map, walkability, occupancy
│       ├── MovementSystem.cs     # fixed-step movement + collision
│       ├── CameraSystem.cs       # IsometricCamera: follow, clamp, world<->screen, iso/flat
│       ├── RenderExtractionSystem.cs  # map -> SpritePacket[] extraction
│       └── Win32TileRenderer.cs  # GDI reference renderer (Windows-only)
│
├── tests/Engine.Tests/           # console smoke tests (NOT a test framework): math, ECS, movement, camera, geometry
│   └── Program.cs                # prints "Smoke tests passed" on success
│
└── tools/
    └── CompileShaders.ps1        # recompiles GLSL -> .spv via glslc (Vulkan SDK Bin on PATH)
```
