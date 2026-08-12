---
name: code-style
description: Applies clean-code conventions: SOLID, KISS, DRY, small single-responsibility types and methods, explicit ownership, file-scoped namespaces, the .editorconfig naming rules, and zero source comments. Use when writing or reviewing code in any project. Do not use for docs, tests, samples, or tooling that intentionally relax style rules.
---

# Code Style

Apply when writing or reviewing source code.

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE SOLID, KISS, and DRY.
- USE small, single-responsibility types and methods with clear names. PREFER explicit ownership over hidden state.
- KEEP it simple: the smallest solution that works. NEVER add speculative abstraction or generality.
- REUSE existing code and contracts instead of duplicating logic. EXTRACT and SHARE any pattern that appears twice.
- ENSURE code is easy to understand, refactor, and support. FOLLOW existing project patterns and conventions.
- APPLY the naming rules in `.editorconfig`: PascalCase types/members, camelCase locals/parameters, `_camel` private fields, `I` interface prefix.
- USE file-scoped namespaces.
- NEVER add comments to source code; write code that is self-documenting.
- APPLY the runtime hot-path rules in the `coding-runtime`, `hot-path-interop`, and `memory-spans` skills when working in hot paths.
