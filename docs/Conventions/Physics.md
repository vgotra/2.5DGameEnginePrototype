# Physics (Jolt / Native)

- ENFORCE a fixed timestep (`TickRate`) for all physics updates to ensure determinism.
- USE native C-API wrappers (e.g., JoltC) via `[LibraryImport]` and `unsafe`.
- USE unmanaged pointers to pass collision shapes and broadphase data to the physics engine.
- FORBID managed object references in physics `UserData`. ONLY store Entity IDs (integers) inside native physics bodies.
