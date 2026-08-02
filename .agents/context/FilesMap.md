# Files Map — repo structure at a glance

Use this to orient quickly; open a file only when your task needs its details. Docs index: `docs/README.md`.

```
2.5DGameEnginePrototype/
├── AGENTS.md                     # AI-agent instructions: commands, shaders, architecture, conventions, principles, platforms
├── opencode.json                 # opencode config: instructions (context + FilesMap), MCP (vulkan + renderdoc servers)
├── README.md                     # project readme (links docs/README.md, release notes, platform status)
├── RELEASE_NOTES.md              # dated change log
├── LICENSE                       # license
├── Engine.sln                    # solution: 13 projects
├── Directory.Build.props         # net10.0, LangVersion preview, warnings-as-errors, nullable, unsafe
├── Directory.Packages.props      # central packages: Vortice.Vulkan 3.2.3
├── global.json                   # SDK 10.0.100 (prerelease allowed)
│
├── .agents/                      # AI-agent assets: skills (engine-development), resumable context (context/), MCP catalog (mcp.json)
├── assets/shaders/               # GLSL sources + committed .spv (recompile via tools/CompileShaders.ps1)
├── docs/                         # documentation: index + topic files (see docs/README.md), agent tooling reference
│
├── src/
│   ├── Engine.Core/              # clock, entity ids, debug metrics — no deps
│   ├── Engine.Mathematics/       # isometric world<->screen math (IsometricMath)
│   ├── Engine.Ecs/               # ECS storage: World, SparseSet, component ids
│   ├── Engine.Threading/         # job system / worker scheduler
│   ├── Engine.Platform/          # platform contracts: IGameWindow, IInputState, PlatformKind, NativeWindowSurface
│   ├── Engine.Platform.Win32/    # Win32 window + input backend
│   ├── Engine.Platform.Desktop/  # GamePlatform host/factory (CreateWindow)
│   ├── Engine.Rendering/         # rendering contracts: IRenderer, sprite/shape packets, generated geometry
│   ├── Engine.Rendering.Vulkan/  # Vulkan backend: renderer, batch draw, pipeline, shader loading, texture infra
│   ├── Engine.Audio/             # audio contracts only (no backend yet)
│   └── Engine.Physics/           # physics contracts only (no backend yet)
│
├── samples/IsometricSandbox/     # executable vertical slice: game loop, tile map, movement/collision, camera, render extraction
├── tests/Engine.Tests/           # console smoke tests (NOT a framework): math, ECS, movement, camera, geometry
└── tools/                        # dev scripts: CompileShaders.ps1 (GLSL -> .spv via glslc), Setup-McpServers.ps1 (clone+build mcp-Vulkan; gitignored tools/mcp/)
```
