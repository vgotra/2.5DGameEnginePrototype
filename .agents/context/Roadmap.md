# Roadmap and Priorities

Implemented milestones live in `Implemented.md`.

## Planned

- **Scene/save format** — non-reflection serialization for tile maps, entities, player state.
- **Desktop platform expansion** — Linux (X11/Wayland) and macOS (SDL3 + MoltenVK): run/verify on the real OSes; no new backend code (SDL3 windowing/input/surface already targets them).
- **Audio backend** — one-shot effects, music streaming, mixer buses, listener/emitter.
- **Physics adapter** — Jolt for continuous collision, bodies, raycasts.
- **Animation and tile atlas support** — sprite animation, atlas metadata, render batching.
- **Debug tools** — collision overlays, entity inspector, frame graph, input visualization.
- **Minimal editor workflow** — after runtime formats and asset loading stabilize.
- **Virtual reality support** — OpenXR HMD (head tracking, per-eye rendering, VR input).
- **Mobile platforms** — Android/iOS via Vulkan + SDL3 + touch input.
- **Frame pacing** — render interpolation and present-mode evaluation for refresh-rate-independent presentation.
- **Shared asset pipeline** — asynchronous loading, unmanaged decode/storage, handles, and sample migration from `TextureLibrary`.
- **Renderer batching** — texture grouping, atlas/bindless evaluation, material support, and reduced descriptor binds.
- **Sparse query ergonomics** — evaluate exclusion/exact-type filters and safe `TryGet` access only when gameplay/editor requirements justify the API expansion.
- **JobSystem capacity** — benchmark the 4096 outstanding-job limit and tiny-job channel churn before changing worker infrastructure.
- **Deferred** — networking, skeletal animation, deferred rendering, consoles, a full editor, production-scale content tooling.
