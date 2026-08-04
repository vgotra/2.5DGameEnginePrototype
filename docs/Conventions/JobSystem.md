# Job System & Parallelism

- ENFORCE dependency graphs for job execution. TREAT this as a roadmap target: the current JobSystem is a provisional queue-based scheduler.
- ENFORCE strict data access rules: Read-Only (Shared) OR Read-Write (Exclusive). NEVER both simultaneously on the same data.
- FORBID `lock` statements. USE lock-free queues, thread-local storage, or `Interlocked` atomic operations.
- USE pre-allocated memory arenas per thread for temporary job allocations.
- AVOID heavy `async/await` in pure game loop ticks. PREFER the custom Job System / ThreadPool.
- ENFORCE single ownership: every shared resource has exactly one owner.
- KEEP window, fixed-step simulation, and render submission on the main thread.
- RUN asset decode, I/O, and parallel work on worker threads.
- NEVER touch renderer state from worker threads. Hand work across threads via jobs or queues.
