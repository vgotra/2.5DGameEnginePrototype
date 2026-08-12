---
name: state-machines
description: Applies gameplay and AI state-machine conventions: finite state machines and utility/behavior scoring, data-driven transitions, polled (not evented) evaluation, and zero-allocation transitions. Use when writing or reviewing gameplay or AI state logic in a game engine. Do not use for pathfinding (see pathfinding) or entity/component architecture (see ecs).
---

# State Machines (Gameplay & AI)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- MODEL gameplay and AI behavior as finite state machines (or utility/behavior scoring) with explicit, data-driven transitions.
- POLL state in systems — evaluate conditions from data, do not push OOP events. SEE `ecs`.
- STORE state and transition tables as value-type structs; transitions allocate nothing. SEE `hot-path-interop`.
- KEEP state updates inside the fixed step for deterministic behavior. SEE `determinism`.
- SPLIT pure state (vectors, timers, flags) from logic; logic lives in static system methods taking state by `ref`. SEE `hot-path-interop`.
