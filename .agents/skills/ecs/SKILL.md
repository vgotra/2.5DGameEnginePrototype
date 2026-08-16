---
name: ecs
description: Applies architecture and ECS (DOD) conventions: value-type components, Entity IDs instead of object references, deferred structural changes via command buffers, systems polling data directly, and scaling up only when a real need appears. Use when writing or reviewing engine architecture, entities, components, or systems. Do not use for tooling, docs, or non-ECS application code.
---

# Architecture & ECS (DOD)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying.

## Rules
- USE Data-Oriented Design (SoA) and strict ECS patterns.
- KEEP the ECS capable of a large entity count (e.g. ~100,000) with parallel multi-component queries, but ONLY when the cost is low and a real need appears. AVOID designs that preclude that scale; NEVER add complexity to reach it before it is needed.
- USE `struct` (value types) for ALL game state and AI logic.
- FORBID classes, OOP polymorphism, and the `new` keyword for per-entity game state in `Update`/`Tick`. Engine infrastructure types (world, systems, backends) may be classes.
- USE Entity IDs (e.g. a `readonly struct` with an `int Id`). FORBID direct object references.
- DELAY structural changes (Add/Remove Entity/Component) to the end of the frame using command buffers.
- FORBID OOP events in systems. Systems must poll/iterate data directly.
