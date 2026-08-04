# Architecture & ECS (DOD)

- USE Data-Oriented Design (SoA) and strict ECS patterns.
- USE `struct` (value types) for ALL game state and AI logic.
- FORBID classes, OOP polymorphism, and the `new` keyword for per-entity game state in `Update`/`Tick`. Engine infrastructure types (World, systems, backends) may be classes.
- USE Entity IDs (e.g., `readonly struct Entity { int Id; }`). FORBID direct object references.
- DELAY structural changes (Add/Remove Entity/Component) to the end of the frame using command buffers.
- FORBID OOP events in systems. Systems must poll/iterate data directly.
