---
name: scenes-memory
description: Applies scene and memory-management conventions: zero-allocation serializers or raw binary layouts, arena (bump) allocators for scene-scoped data, and freeing the whole arena at once on teardown. Use when writing or reviewing scene loading, serialization, or memory management code in a game engine. Do not use for general asset I/O (see assets-io).
---

# Scenes & Memory Management

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- USE zero-allocation serializers (e.g. FlatBuffers, MemoryPack) or raw binary layouts for scene loading.
- USE arena allocators (bump allocators) for scene-scoped data.
- FREE the entire native memory arena at once upon scene teardown. NEVER track individual allocations for destruction.
