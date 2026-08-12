---
name: persistence-config
description: Applies persistence and settings conventions: save/load game state and configuration, zero-allocation serialization, versioned schemas, and atomic writes. Use when writing or reviewing save systems, settings, or configuration code. Do not use for scene loading (see scenes-memory) or logging (see logging).
---

# Persistence & Configuration

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- SERIALIZE with zero-allocation serializers (e.g. FlatBuffers, MemoryPack) or raw binary layouts. SEE `scenes-memory`.
- VERSION every saved payload and config schema; migrate explicitly on load.
- WRITE atomically: write to a temp file, then rename — never leave a torn save.
- KEEP I/O off the main thread; do not block the loop on save/load. SEE `job-system`.
- STORE settings data-driven and validated at load; fail softly with defaults on corruption.
- SEPARATE volatile game state from persistent settings; never write per-frame state to disk.
