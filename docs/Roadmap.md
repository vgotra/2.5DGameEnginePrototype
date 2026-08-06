# Roadmap and Priorities

## Next features, ordered by usefulness

1. **Texture sampling path** — sample textures in the fragment shader, bind descriptor sets per sprite batch, texture atlas support, and honor `SpritePacket.Texture`/`Material`.
2. **Asset loading** — PNG decoding, texture upload, sprite handles, and a small `assets/` convention.
3. **ECS queries and system scheduling** — replace sample-local state with ECS systems and explicit read/write access. Target scale: ~100k entities with parallel multi-component queries.
4. **Profiling and allocation metrics** — frame timings, draw calls, jobs, GC bytes, and Vulkan timestamps.
5. **Job dependencies and safe parallel work** — dependency-aware jobs for asset loading, large-map extraction, and uploads.
6. **Scene/save format** — explicit non-reflection serialization for tile maps, entities, and player state.
7. **Audio backend** — one-shot effects, music streaming, mixer buses, and listener/emitter support.
8. **Physics adapter** — integrate Jolt only when gameplay needs continuous collision, bodies, or raycasts.
9. **Animation and tile atlas support** — sprite animation, atlas metadata, and render batching.
10. **Debug tools** — collision overlays, entity inspector, frame graph, and input visualization.
11. **Minimal editor workflow** — only after runtime formats and asset loading are stable.
12. ~~**SDL3 platform backend (replaces SDL2)**~~ — **DONE (SDL3 migration milestone):** windowing/input and the Vulkan surface now run on **SDL3** (`ppy.SDL3-CS`), the native Win32 path is deleted, and `GamePlatform.CreateWindow` uses SDL3 on all OSes. Remaining: verify X11/Wayland on Linux and macOS via SDL3 + MoltenVK (see `docs/LinuxSupportPlan.md`).
13. **Shader compilation with progress + splash screen** — replace `tools\CompileShaders.ps1` (PowerShell + `glslc` subprocess) with in-process C# shader compilation that works on **all platforms** (see `docs/ShaderWorkflow.md`). At startup a splash screen checks whether any `assets/shaders/*.glsl` is newer than the committed `shaders/*.spv` (recompilation needed) and, when it is, recompiles them showing per-shader progress before the game window opens.
14. **Virtual reality (VR) support** — OpenXR-based HMD integration (head tracking, per-eye rendering, VR input) layered on top of the Vulkan render path.
15. **Mobile platforms support** — Android and iOS with the Vulkan renderer, SDL3 windowing, and touch input (promoted from the deferred list).

## Deliberately deferred

Networking, skeletal animation, deferred rendering, consoles, a full editor, and production-scale content tooling are not MVP priorities.
