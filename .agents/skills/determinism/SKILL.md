---
name: determinism
description: Enforces fixed-step simulation determinism: a fixed timestep accumulator, no wall-clock/random/hash ordering in simulation, main-thread movement and collision, and stable identifiers. Use when writing or reviewing simulation, movement, or physics code in a game engine. Do not use for rendering, tooling, or non-simulation code.
---

# Determinism (Fixed-Step Simulation)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE a fixed timestep (e.g. `<TickRate>` = 60 Hz) driven by the clock accumulator. NEVER advance simulation with per-frame wall-clock `dt`.
- FORBID `Random`, `DateTime.Now`, `Environment.TickCount`, and other wall-clock reads inside fixed-step simulation. USE deterministic math only.
- KEEP fixed-step player movement and collision on the main thread for deterministic ordering.
- USE stable identifiers (entity index + generation, component type id counter) for ordering. NEVER depend on runtime hash order (`string.GetHashCode`, dictionary/enumeration order).
