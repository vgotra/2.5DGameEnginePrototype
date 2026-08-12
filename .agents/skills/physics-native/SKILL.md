---
name: physics-native
description: Applies native physics conventions: fixed timestep, native C-API wrappers via [LibraryImport] and unsafe, unmanaged pointers for collision shapes, and integer Entity IDs in native body user data instead of managed references. Use when writing or reviewing physics code in a game engine. Do not use for pure managed math or non-physics code.
---

# Physics (Native)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE a fixed timestep for all physics updates to ensure determinism.
- USE native C-API wrappers (e.g. `<PhysicsLib>`) via `[LibraryImport]` and `unsafe`.
- USE unmanaged pointers to pass collision shapes and broadphase data to the physics engine.
- FORBID managed object references in physics `UserData`. ONLY store Entity IDs (integers) inside native physics bodies.
