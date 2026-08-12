---
name: codegen-generators
description: Applies .NET source-generator conventions: compile-time code generation for ECS, interop, and serialization that is AOT/trimming-safe and allocation-free at runtime. Use when writing or reviewing source generators or generated-code contracts in a .NET project. Do not use for runtime reflection-based generation or dynamic code.
---

# Source Generators & Codegen

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- GENERATE at compile time with .NET source generators for ECS queries, interop marshalling, and serialization; never reflect or emit at runtime. SEE `publish-native-mobile`.
- KEEP generated code AOT/trimming-safe: no reflection, no dynamic loading, no runtime codegen. SEE `coding-runtime`.
- MAKE generation deterministic from source inputs; identical source → identical generated code.
- EMIT code that follows the same hot-path rules as hand-written code (no allocations, spans, by-ref). SEE `hot-path-interop`.
- SHIP generated files into the build rather than committing them where possible; keep the contracts stable.
