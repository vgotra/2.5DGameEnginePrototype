---
name: debugging-native
description: Applies debugging conventions for native interop and crash analysis: dump/stack analysis, memory and handle validation, and structured diagnostics. Use when debugging crashes, memory corruption, or native-interop issues in a project with unmanaged code. Do not use for normal feature debugging or logging (see logging).
---

# Debugging (Native & Interop)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- REPRODUCE deterministically: capture the failing input/state, not just the symptom. SEE `determinism`.
- ANALYZE crash dumps (stack, native/managed boundary) rather than guessing from the error text.
- VALIDATE native boundaries: check handles/pointers at the interop seam; blittable-only types prevent marshalling bugs. SEE `hot-path-interop`.
- CHECK memory ownership and frees on the debug path; enable allocator checks in debug builds. SEE `memory-spans`.
- USE structured diagnostics (log categories, counters) to narrow the failing subsystem. SEE `logging`, `profiling-diagnostics`.
- BISECT changes: a regression points at the last diff that changed behavior; verify with the build/verify loop. SEE `build-and-verify`.
