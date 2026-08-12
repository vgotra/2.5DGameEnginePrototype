---
name: logging
description: Applies logging conventions: structured, category- and level-based logging with zero allocation in hot paths and no logging inside the fixed step. Use when writing or reviewing logging, diagnostics, or error reporting code. Do not use for in-app metrics (see profiling-diagnostics) or for deciding error-handling policy (see coding-runtime).
---

# Logging

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- USE structured logging with categories and verbosity levels; route per category, never a single firehose.
- FORBID logging inside hot loops and the fixed-step simulation; logging is for boundaries, startup, and errors. SEE `coding-runtime`.
- DEFER formatting: pass structured fields, not pre-formatted strings, and keep steady-state allocation at zero. SEE `hot-path-interop`.
- NEVER use logs for control flow; report failures via return codes and log at the boundary. SEE `coding-runtime`.
- GUARD log calls so disabled levels cost nothing.
