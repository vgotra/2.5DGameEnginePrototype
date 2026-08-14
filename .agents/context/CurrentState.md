# Current State

## Status

- Windows-first .NET 10 2.5D isometric engine prototype.
- Vulkan is the only renderer; SDL3 provides windowing, input, and Vulkan surfaces.
- Engine.Ecs.Sparse is the canonical ECS implementation.
- IsometricSandbox runs on sparse entities, optional parallel sparse queries, deferred mutation, and the sparse frame scheduler.
- Rendering remains separate from gameplay through renderer-neutral sprite extraction and Vulkan submission.
- Gameplay supports ability/weapon/projectile damage flow, reusable NPC behavior state, and pooled fixed-step VFX.

## Active Constraints

- Sparse queries support one-, two-, and three-component intersections with opt-in parallel execution.
- Fixed-step movement and collision remain deterministic and main-thread driven.
- Asset decoding is owned by `Engine.Assets` in unmanaged storage; Vulkan texture uploads are render-thread-owned, fence-backed, and publish handles only after completion.
- Vulkan validation and real Linux/macOS runs remain environment-dependent.
- Live batch command recording is serial after the Milestone 13 rendering audit; parallel extraction remains available where measured useful.
- Material handles select descriptor-backed texture resources; indexed mode uses stable descriptor-array indices with per-texture-set fallback for unsupported devices. Independent shader-parameter material assets are not implemented.
- Animation frames currently use eight horizontal atlas cells when a non-zero frame is supplied.
- The default Release benchmark catalog contains 35 representative cases; timing changes are diagnostic warnings and allocation regressions remain failures.

## Next Actions

- Known limitations and their follow-up candidates are recorded in KnownIssues.md and Roadmap.md.

## Resume Rules

- Read CompletedMilestones.md for milestone history.
- Read Implemented.md for the brief shipped-feature inventory.
- Read KnownIssues.md for active limitations.
