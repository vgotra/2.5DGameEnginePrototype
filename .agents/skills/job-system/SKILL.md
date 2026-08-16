---
name: job-system
description: Applies JobSystem and parallelism conventions: explicit barriers, disjoint partitions, no locks (lock-free queues, TLS, Interlocked), per-thread arenas, single ownership, and main-thread ownership of window/simulation/present state. Use when writing or reviewing parallel jobs or threading code in a game engine. Do not use for tooling, tests, or single-threaded application code.
---

# Job System & Parallelism

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying.

## Rules
- ENFORCE caller-owned ordering with explicit waits and completion barriers. Do not infer dependency graphs or require a general DAG. Parallel jobs write disjoint partitions (bands/chunks); merges happen once on the main thread after the barrier.
- ENFORCE strict data access rules: Read-Only (Shared) OR Read-Write (Exclusive). NEVER both simultaneously on the same data.
- FORBID `lock` statements. USE lock-free queues, thread-local storage, or `Interlocked` atomic operations.
- USE pre-allocated memory arenas per thread for temporary job allocations. REUSE cached delegates and pooled closures so steady-state dispatch allocates nothing.
- AVOID heavy `async/await` in pure game loop ticks. PREFER the custom job system / thread pool. (An async drain exists for tooling/tests; the frame loop uses the synchronous complete path.)
- ENFORCE single ownership: every shared resource has exactly one owner.
- KEEP window, fixed-step simulation, queue submit/present, and all renderer-owned state on the main thread.
- RUN asset decode, I/O, and parallel work on worker threads. GPU uploads and staging copies stay on the main thread; workers decode into result slots and the main thread uploads after completion.
- ALLOW worker threads to RECORD graphics API secondary command buffers via a parallel recorder (one command pool per chunk slot per frame-in-flight, reset after the frame-slot fence). Chunks are contiguous range partitions executed in order, preserving painter-order blending exactly. Workers never mutate renderer state objects.
