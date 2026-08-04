# Scenes & Memory Management

- USE zero-allocation serializers (e.g., FlatBuffers, MemoryPack) or raw binary layouts for scene loading.
- USE arena allocators (bump allocators) for scene-scoped data.
- FREE the entire native memory arena at once upon scene teardown. NEVER track individual allocations for destruction.
