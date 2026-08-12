# Agent Skills

Portable, just-in-time-loaded agent instructions. Each skill is a standalone `SKILL.md` under its own folder; opencode discovers them at startup (`name` must match the folder, lowercase-hyphenated).

## Using these skills

Every skill is **generic** — it uses `<...>` placeholders instead of concrete project or solution names so it can be reused across projects of the same type. Before applying a skill, substitute the placeholders with the actual repo's names (from `.agents/context/ProjectConfig.md`).

## Placeholder glossary

| Placeholder | Meaning |
|---|---|
| `<ProjectName>` | Product/repo/project name (namespace root, assembly name) |
| `<SolutionName>` | Solution file base name (e.g. `Solution` in `Solution.slnx`) |
| `<TestProject>` | Smoke-test project path (e.g. `tests/<ProjectName>.Tests/<ProjectName>.Tests.csproj`) |
| `<SampleProject>` | Sample project path |
| `<BenchmarkProject>` | Benchmark project path |
| `<GraphicsApi>` | Graphics API (e.g. Vulkan, Direct3D) |
| `<PhysicsLib>` | Native physics library (e.g. JoltC) |
| `<ImageDecodeLib>` | Native image decoding library (e.g. stb_image) |
| `<ShaderCompiler>` | Shader compiler tool (e.g. glslc) |
| `<ShaderSourcesDir>` | Shader source directory (e.g. `assets/shaders`) |
| `<TickRate>` | Fixed timestep (e.g. 60 Hz) |
| `<ResultsDir>` | Benchmark results directory (e.g. `benchmarks/results`) |
| `<ManualShaderCompileScript>` | Manual shader-compile fallback script path (e.g. `tools/CompileShaders.ps1`) |
| `<ShaderRequiredProperty>` | MSBuild property that makes missing shader tooling a hard build error (e.g. `/p:ShadersRequired=true`) |
| `<PresentMode>` | Swapchain present/latency mode (e.g. MAILBOX, FIFO) |

Other concrete contracts (types, projects, seams) are defined in `.agents/context/ProjectConfig.md`, not the glossary — skills placehold-only what they reference.

## Skills

### Generic (any project type)

- `build-and-verify` — post-change verification loop and benchmark methodology (build, smoke tests, sample run, benchmark gate).
- `code-review` — review checklist against repo conventions.
- `code-style` — SOLID/KISS/DRY, naming, explicit ownership, zero comments.
- `codegen-generators` — AOT-safe .NET source generators for ECS/interop/serialization.
- `coding-runtime` — no reflection/LINQ/allocations in hot paths, return-code errors.
- `debugging-native` — crash dumps, native-interop debugging, memory validation.
- `engine-api-layering` — engine/game/sample boundaries, public contract design, versioning.
- `hot-path-interop` — spans, by-ref passing, SoA, `[LibraryImport]` native interop.
- `localization` — string tables, locale fallback, zero-alloc lookups.
- `logging` — structured, zero-allocation, category/level-based logging.
- `math` — aggressive inlining, branchless programming.
- `memory-spans` — spans/pointers, `NativeMemory`, no LINQ/`foreach` in hot paths.
- `object-pooling` — fixed-capacity reusable pools, zero steady-state allocation.
- `packaging-build` — central package management, shared build props.
- `performance-budget` — per-frame time budgets, adaptive resolution/quality.
- `persistence-config` — save/settings, zero-alloc serialization, atomic writes.
- `platform-neutrality` — OS-specific code confined to platform seams (window/input/surface factory).
- `profiling-diagnostics` — in-app frame metrics, counters, allocation tracking, external tools.
- `publish-native-mobile` — NativeAOT/trimming-safe code, mobile publish, size budgets.
- `resumable-context` — updating resumable-state files after a milestone.

### Game engine — core

- `animations-2-5d` — 2.5D isometric animation conventions.
- `animations-2d` — 2D animation conventions.
- `asset-pipeline` — build-time import/cook, deterministic outputs, dev hot-reload.
- `assets-io` — async asset loading, unmanaged decode, handles, atlases, authorship (alpha/filter/geometry).
- `determinism` — fixed-step simulation determinism.
- `ecs` — architecture & ECS (DOD): struct components, Entity IDs, command buffers.
- `game-loop-frame` — system execution order, fixed-step + render interpolation, pacing.
- `job-system` — dependency graphs, shared/exclusive access, lock-free threading.
- `physics-native` — native physics via C-API wrappers, Entity IDs in body user data.
- `pre-generation-checklist` — pre-generation verification for runtime code.
- `scenes-memory` — zero-alloc serializers, arena allocators.
- `shader-workflow` — shader compile/deploy, compiler probing/fallback, and coordinate-system rules.
- `simd` — hardware intrinsics for batch tile and physics processing.

### Game engine — rendering & presentation

- `camera-view` — ortho camera, world/screen transforms, follow/shake/zoom.
- `lighting-2d` — 2D/2.5D lighting, normal maps, light sources.
- `particles-effects` — pooled, GPU-batched particle systems.
- `rendering-batching` — sprite/instance batching, draw order, bind minimization.
- `shader-effects` — post-processing, blending, uniform/UBO parameter passing.
- `swapchain-lifecycle` — swapchain creation, acquire/draw/present, resize/fullscreen recreation.
- `text-rendering` — glyph atlases, font metrics, SDF fonts.
- `tilemap-rendering` — chunked tile-grid rendering, visibility, budgeted updates.
- `ui-system` — widget layout, 9-slice, input-driven widgets, polled updates.

### Game engine — simulation & gameplay

- `audio` — clip handles, pooled voices, deterministic mixing.
- `behavior-ai` — behavior trees / utility AI, zero-alloc ticks.
- `collision-2d` — AABB/OBB/circle narrowphase, swept collision, broadphase reuse.
- `culling-spatial` — uniform grid/quadtree broadphase, range/ray queries.
- `input-action-mapping` — device-agnostic actions, action maps, touch gestures.
- `pathfinding` — grid A*/flow fields, path caching, agent steering.
- `procedural-generation` — seeded deterministic noise, tile/world generation.
- `state-machines` — gameplay/AI FSMs, polled evaluation.
- `tweening` — pooled tweens, easing, fixed-step driven.
