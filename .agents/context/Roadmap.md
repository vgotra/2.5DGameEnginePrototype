# Roadmap and Priorities

The runtime architecture is implemented and recorded in `CurrentState.md`, `Implemented.md`, and `Simplified Architecture v2.md`. Verified history belongs in `CompletedMilestones.md`.

## Planned

- **Material system** — independent shader parameters, material assets, and renderer-neutral material handles.
- **Presentation policy** — measure FIFO, MAILBOX, and immediate present behavior and add explicit frame-pacing selection where justified.
- **Texture lifetime** — eviction, descriptor-index retirement, atlas repacking, and cooked texture/atlas assets.
- **Audio runtime** — clip assets, pooled voices, mixing, and platform backend integration.
- **Physics integration** — expand the existing native physics seam into runtime gameplay contracts.
- **Persistence and tooling** — save serialization, editor workflows, and asset inspection tools.
- **JobSystem capacity** — measure 4096-job saturation and tiny-job channel churn before changing worker infrastructure.

## Rules

- Preserve renderer-neutral contracts and deterministic painter ordering.
- Keep Vulkan and SDL3 details behind their existing platform seams.
- Add a roadmap item only when it has a concrete acceptance target and is not already implemented.
