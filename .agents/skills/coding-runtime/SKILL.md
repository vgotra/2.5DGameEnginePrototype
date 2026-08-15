---
name: coding-runtime
description: Applies runtime coding constraints: zero reflection, LINQ, and managed allocation in hot paths, separated allocation domains, targeted parallelization, main-thread fixed-step determinism, and return-code error handling instead of exceptions. Use when writing or reviewing runtime/game-loop code. Do not use for docs, tests, tools, or samples that intentionally relax these rules.
---

# Coding (Runtime)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE zero reflection, LINQ, and managed allocation in runtime hot paths. USE structs, spans, explicit loops, and preallocated buffers.
- SEPARATE startup, frame, and persistent allocation domains. TREAT zero-GC as a steady-state target, not an unverified promise.
- PARALLELIZE only asset decoding, shader compilation, uploads, and world extraction for maps above roughly 10,000 visible tiles.
- KEEP fixed-step player movement and collision on the main thread for deterministic ordering.
- FORBID exceptions in hot paths and fixed-step simulation. USE return codes (e.g. the graphics API result type, `bool`). THROW only at subsystem boundaries (setup, validation, unrecoverable).
- PRESERVE descriptive names and named constants in runtime and hot-path code. Zero-allocation, fixed-step, and performance constraints do not justify cryptic abbreviations or unexplained magic values.
