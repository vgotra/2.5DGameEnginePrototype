---
name: asset-pipeline
description: Applies build-time asset pipeline conventions: importing and cooking assets (texture/atlas packing, audio conversion, shader compile), deterministic outputs, and developer hot-reload. Use when writing or reviewing asset tooling, importers, or build-time asset steps in a game engine. Do not use for runtime asset loading (see assets-io) or shader compile workflow (see shader-workflow).
---

# Asset Pipeline

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying.

## Rules
- COOK assets at build/import time into runtime-friendly formats (atlases, packed audio, compiled shaders); the runtime loads cooked data, not source files. SEE `assets-io`.
- MAKE outputs deterministic: identical source + tooling → identical bytes, so builds are reproducible.
- PACK atlases and group textures to minimize runtime binds. SEE `assets-io`, `rendering-batching`.
- INVALIDATE incrementally: rebuild only assets whose source changed (content-address or timestamp based), like the shader compile target. SEE `shader-workflow`.
- SUPPORT dev hot-reload: rebuild + reload assets without restarting when practical; gate it out of release builds.
- FAIL the build on missing tooling (e.g. a `ShadersRequired`-style gate) instead of shipping stale assets.
