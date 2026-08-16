---
name: engine-api-layering
description: Applies engine-layer and API-design conventions: engine vs game vs sample boundaries, public contract design, and versioning. Use when designing, reviewing, or changing the public surface of an engine or library. Do not use for platform-specific backend code (see platform-neutrality) or for writing gameplay features.
---

# Engine API Layering

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying.

## Rules
- SEPARATE engine, game, and sample/project layers; the engine exposes contracts, games implement content, samples demonstrate.
- KEEP the public surface minimal and stable: small interfaces/contracts, no OS or backend types leaking out. SEE `platform-neutrality`.
- DESIGN contracts to be AOT/trimming-safe and hot-path friendly (structs, spans, IDs). SEE `hot-path-interop`.
- VERSION the public API explicitly; breaking changes bump the major version and are documented.
- REUSE one contract per concern (DRY); never duplicate an interface for an alternate backend. SEE `code-style`.
