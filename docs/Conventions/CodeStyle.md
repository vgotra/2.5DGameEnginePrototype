# Code Style

- ENFORCE SOLID, KISS, and DRY.
- USE small, single-responsibility types and methods with clear names. PREFER explicit ownership over hidden state.
- KEEP it simple: the smallest solution that works. NEVER add speculative abstraction or generality.
- REUSE existing code and contracts instead of duplicating logic. EXTRACT and SHARE any pattern that appears twice.
- ENSURE code is easy to understand, refactor, and support. FOLLOW existing project patterns and conventions.
- APPLY the naming rules in `.editorconfig`: PascalCase types/members, camelCase locals/parameters, `_camel` private fields, `I` interface prefix.
- USE file-scoped namespaces.
- NEVER add comments to source code; write code that is self-documenting.
- APPLY the runtime hot-path rules in `Coding.md`.
