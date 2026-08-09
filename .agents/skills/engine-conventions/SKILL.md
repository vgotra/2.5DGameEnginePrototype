---
name: engine-conventions
description: Applies the repo's coding and hot-path conventions when writing or reviewing engine code: 1 type = 1 file, no reflection/LINQ/allocations in runtime projects, platform-neutral contracts, zero comments, explicit ownership. Use when generating new engine code or reviewing a change. Do not use for tools/, docs, tests, or samples that intentionally relax rules.
---

## What I do

Apply the pre-generation checklist and platform/convention rules. The authoritative versions are `docs/Conventions/Checklist.md` and `docs/Conventions/Restrictions.md`.

## Pre-generation checklist (from `docs/Conventions/Checklist.md`) — all MUST BE YES

1. Zero hidden heap allocations (`new`, boxing, closures, strings) in runtime code.
2. Branchless logic prioritized over `if/else` in loops.
3. All native buffers explicitly freed.
4. ECS systems process raw pointers/spans, not objects.
5. Multi-threading avoids `lock` and respects exclusive write access.
6. Physics and assets rely on Handles/IDs, not managed references.
7. Zero comments in source code.

## Principles and platform neutrality

- SOLID, KISS, DRY: small single-responsibility types and methods; the smallest solution that works; no speculative abstraction; follow existing patterns. 1 type = 1 file repo-wide (enums and 1-line handles included).
- No reflection/LINQ/`foreach` in runtime hot paths; structs/spans/by-ref passing and explicit loops. See `docs/Conventions/Coding.md`, `HotPath.md`, `MemorySpans.md`.
- Platform neutrality (`docs/Conventions/Restrictions.md`): FORBID OS-specific P/Invoke and types in contracts and shared gameplay code; OS-specific code lives in `Engine.Platform.*` backends; the renderer consumes `NativeWindowSurface` + an `IVulkanSurfaceFactory` (never an OS surface struct); backend selection happens in `Engine.Platform.Desktop.GamePlatform` (SDL3 on all OSes); never add OS-specific parameters to contracts or `IRenderer`.
