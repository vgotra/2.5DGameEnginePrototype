---
name: object-pooling
description: Applies object pooling conventions: fixed-capacity reusable pools, preallocated acquire/release, explicit return-to-pool, single ownership, and zero steady-state allocation. Use when writing or reviewing any pooling, spawner, or high-frequency object lifecycle code. Do not use for scene-scoped arenas (see scenes-memory) or for explaining what a pool does — this is about how to implement one.
---

# Object Pooling

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- POOL any object created/destroyed frequently in the hot path (projectiles, particles, UI nodes, tween handles, voices).
- PREALLOCATE fixed capacity up front; acquire returns a struct/ref to a slot, release returns it explicitly. SEE `hot-path-interop`.
- FORBID per-op `new`/closure/boxing; steady-state acquire/release allocates nothing.
- ENFORCE single ownership: exactly one owner per pooled object; release clears the slot deterministically.
- NEITHER rely on finalizers nor on GC for return — callers return objects explicitly.
- USE value-type slots (SoA where possible) to avoid reference-chasing. SEE `hot-path-interop`.
- FREE native pools fully at teardown; never leak slots. SEE `memory-spans`.
