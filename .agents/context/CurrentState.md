# Current State

## Status

- Windows-first .NET 10 2D/2.5D isometric engine prototype.
- Vulkan is the only renderer; SDL3 provides windowing, input, and Vulkan surfaces.
- Engine.Ecs.Sparse is the canonical ECS implementation.
- IsometricSandbox runs on sparse entities, serial sparse queries, deferred mutation, and the sparse frame scheduler.
- Rendering remains separate from gameplay through sprite extraction and Vulkan submission.

## Active Constraints

- Sparse queries support one-, two-, and three-component intersections; parallel query execution is not implemented yet.
- Fixed-step movement and collision remain deterministic and main-thread driven.
- Asset decoding and texture ownership remain sample-local and managed.
- Vulkan validation and real Linux/macOS runs remain environment-dependent.

## Next Actions

- Milestone 7: add deliberate multithreaded sparse queries with serial/parallel parity, determinism, and benchmark thresholds.
- Roadmap follow-ups remain in Roadmap.md.

## Resume Rules

- Read CompletedMilestones.md for milestone history.
- Read Implemented.md for the brief shipped-feature inventory.
- Read KnownIssues.md for active limitations.
