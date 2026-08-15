# Roadmap and Priorities

The runtime architecture is implemented and recorded in `CurrentState.md`, `Implemented.md`, and `Simplified Architecture v2.md`. Verified history belongs in `CompletedMilestones.md`.

## Follow-up Work

- **Material system** — independent shader parameters, material assets, and renderer-neutral material handles.
- **Texture lifetime** — eviction, descriptor-index retirement, atlas repacking, and cooked texture/atlas assets.
- **Audio runtime** — clip assets, pooled voices, mixing, and platform backend integration.
- **Physics integration** — expand the existing native physics seam into runtime gameplay contracts.
- **Persistence and tooling** — save serialization, editor workflows, and asset inspection tools.
- **Platform verification** — validate Linux SDL3/Vulkan and macOS SDL3/MoltenVK targets.

## Deferred Until Required

Behavior trees, GOAP, complex FSM frameworks, network replication, editor tooling, visual scripting, reflection-heavy DI, generic scripting VMs, large archetype hierarchies, complex animation graphs, excessive stat systems, procedural world generation, generic modding, and broad serialization frameworks remain out of scope until a concrete feature requires them.

---

## Rules

- Preserve renderer-neutral contracts and deterministic painter ordering.
- Keep Vulkan and SDL3 details behind their existing platform seams.
- Add a roadmap item only when it has a concrete acceptance target and is not already implemented.
