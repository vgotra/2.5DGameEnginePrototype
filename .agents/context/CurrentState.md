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
- Asset decoding and texture ownership remain sample-local and managed.
- Vulkan validation and real Linux/macOS runs remain environment-dependent.
- Live batch command recording is serial after the Milestone 13 rendering audit; parallel extraction remains available where measured useful.
- Material handles currently select descriptor-backed texture resources; independent shader-parameter material assets are not implemented.
- Animation frames currently use eight horizontal atlas cells when a non-zero frame is supplied.

## Next Actions

- Known limitations and their follow-up candidates are recorded in KnownIssues.md and Roadmap.md.

## Resume Rules

- Read CompletedMilestones.md for milestone history.
- Read Implemented.md for the brief shipped-feature inventory.
- Read KnownIssues.md for active limitations.
