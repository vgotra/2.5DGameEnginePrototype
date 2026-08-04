# Determinism (Fixed-Step Simulation)

- ENFORCE a fixed timestep (60 Hz) driven by the `GameClock` accumulator. NEVER advance simulation with per-frame wall-clock `dt`.
- FORBID `Random`, `DateTime.Now`, `Environment.TickCount`, and other wall-clock reads inside fixed-step simulation. USE deterministic math only.
- KEEP fixed-step player movement and collision on the main thread for deterministic ordering.
- USE stable identifiers (`EntityId` index + generation, `ComponentTypeId` counter) for ordering. NEVER depend on runtime hash order (`string.GetHashCode`, dictionary/enumeration order).
