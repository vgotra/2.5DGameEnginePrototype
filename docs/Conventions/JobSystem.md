# Job System & Parallelism

- ENFORCE dependency graphs for job execution (`JobSystem.Schedule(action, deps)`, `ScheduleFor` barriers; ≤8 dependencies per job).
- ENFORCE strict data access rules: Read-Only (Shared) OR Read-Write (Exclusive). NEVER both simultaneously on the same data. Parallel jobs write disjoint partitions (bands/chunks); merges happen once on the main thread after the barrier.
- FORBID `lock` statements. USE lock-free queues, thread-local storage, or `Interlocked` atomic operations.
- USE pre-allocated memory arenas per thread for temporary job allocations. REUSE cached delegates and pooled closures (`ParallelForChunk` pool) so steady-state dispatch allocates nothing.
- AVOID heavy `async/await` in pure game loop ticks. PREFER the custom Job System / ThreadPool. (`DrainAsync` exists for tooling/tests; the frame loop uses `Complete`.)
- ENFORCE single ownership: every shared resource has exactly one owner.
- KEEP window, fixed-step simulation, queue submit/present, and all renderer-owned state on the main thread.
- RUN asset decode, I/O, and parallel work on worker threads. GPU uploads (`UploadTexture`, staging copies) stay on the main thread; workers decode into result slots and the main thread uploads after `IsComplete`.
- ALLOW worker threads to RECORD Vulkan secondary command buffers via `ParallelDrawRecorder` (one command pool per chunk slot per frame-in-flight, reset after the frame-slot fence). Chunks are contiguous range partitions executed in order with `vkCmdExecuteCommands`, preserving painter-order blending exactly. Workers never mutate renderer state objects.
