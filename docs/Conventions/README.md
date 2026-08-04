# Conventions

Coding, performance, packaging, and build conventions that differ from plain .NET defaults. Principles are in `../../AGENTS.md` (`## Principles`).

Future-system rules (JobSystem dependency graphs, SIMD, Jolt physics, scenes/arena allocators, bindless/atlases, animations, `NativeMemory`) are roadmap targets; rules on shipped code are current policy.

- [`CodeStyle.md`](CodeStyle.md) — code style: SOLID/KISS/DRY, naming, ownership, simplicity, reusability.
- [`Coding.md`](Coding.md) — runtime hot-path rules: no reflection/LINQ/allocations, structs/spans/explicit loops, allocation domains, parallelization, main-thread determinism, exception policy.
- [`HotPath.md`](HotPath.md) — hot-path, AI-agent, and native-interop rules: SoA, spans, by-ref passing, method splitting, `[LibraryImport]`/`[SuppressGCTransition]` for OS/native-C P/Invoke.
- [`Determinism.md`](Determinism.md) — fixed-step simulation determinism: `GameClock` accumulator, no wall-clock/random/hash ordering in sim.
- [`Packaging.md`](Packaging.md) — central package management, `Directory.Build.props` globals, and project layout.
- [`Restrictions.md`](Restrictions.md) — platform neutrality: what contracts and shared code must not contain.
- [`Commands.md`](Commands.md) — build, test, run, and shader-recompile commands.
- [`VulkanInterop.md`](VulkanInterop.md) — Vulkan/native interop: Vortice binding, `[LibraryImport]`, blittable types, `VkResult`, native struct layout.
- [`MemorySpans.md`](MemorySpans.md) — memory and span processing: unsafe/raw pointers, spans, `NativeMemory`, by-ref passing, no LINQ/`foreach` in hot paths.
- [`JobSystem.md`](JobSystem.md) — job system and parallelism: dependency graphs, access rules, lock-free concurrency, thread ownership.
- [`Ecs.md`](Ecs.md) — architecture and ECS (DOD): structs, Entity IDs, command buffers.
- [`Animations.md`](Animations.md) — 2.5D isometric animations: struct components, SIMD/job batching, GPU frame calculation.
- [`Animations2D.md`](Animations2D.md) — 2D (`--2d`) animations: struct components, batched updates, GPU frame calculation.
- [`Physics.md`](Physics.md) — physics (Jolt/native): fixed timestep, C-API wrappers, integer Entity IDs in native bodies.
- [`Assets.md`](Assets.md) — assets, textures, and I/O: async loading, unmanaged decode, asset handles, atlases/bindless.
- [`Scenes.md`](Scenes.md) — scenes and memory management: zero-allocation serializers, arena allocators.
- [`Math.md`](Math.md) — math helpers: inlining, branchless programming.
- [`Simd.md`](Simd.md) — SIMD: hardware intrinsics for batch tile and physics processing.
- [`Checklist.md`](Checklist.md) — pre-generation AI checklist.
