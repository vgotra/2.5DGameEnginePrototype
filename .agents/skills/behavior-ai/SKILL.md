---
name: behavior-ai
description: Applies behavior-tree and utility-AI conventions: decision trees over state, scored options, deterministic evaluation, and zero-allocation ticks. Use when writing or reviewing advanced AI decision logic beyond simple FSMs in a game engine. Do not use for simple finite state machines (see state-machines) or pathfinding (see pathfinding).
---

# Behavior Trees & Utility AI

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- MODEL decisions as behavior trees or utility scoring over agent state; keep trees/data-driven, not hard-coded control flow.
- EVALUATE deterministically in the fixed step; identical state → identical decision. SEE `determinism`.
- POLL state in systems; no OOP events or callbacks between tree nodes. SEE `ecs`, `state-machines`.
- STORE nodes as value-type structs; tick allocates nothing. SEE `hot-path-interop`.
- COMPOSE with the FSM skill where a simple state machine fits better. SEE `state-machines`.
