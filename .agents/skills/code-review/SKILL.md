---
name: code-review
description: Applies a review checklist when reviewing code: correctness, the repo's conventions (style, hot-path, platform seams), tests/verification, and clarity. Use when reviewing a change or PR in any project. Do not use when writing new code (that is development work).
---

# Code Review

## Apply
- Review against this repo's conventions, not generic best practices alone. Substitute `<...>` placeholders where needed.

## Checklist
- CORRECTNESS: does it do what it claims, including edge cases and failure paths?
- CONVENTIONS: does it follow `code-style`, and where relevant `coding-runtime`, `hot-path-interop`, `memory-spans`?
- HOT PATHS: no new allocations, LINQ/reflection, or `foreach` in hot loops; spans and by-ref used. SEE `pre-generation-checklist`.
- PLATFORM: no OS-specific types in shared/contract code. SEE `platform-neutrality`.
- SIMPLICITY: smallest change that works; no speculative abstraction. SEE `code-style`.
- VERIFICATION: does the change build and pass the verify loop? SEE `build-and-verify`.
- CLARITY: names and structure make intent obvious; zero comments needed.
