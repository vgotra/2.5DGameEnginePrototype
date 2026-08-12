---
name: assets-io
description: Applies asset, texture, and I/O conventions: asynchronous background loading, unmanaged image decoding, Asset IDs/Handles instead of raw pointers, texture atlases/bindless textures to minimize state changes and pipeline binds, and asset authorship (alpha, filtering, geometry). Use when writing or reviewing asset loading, decoding, texture, or art-integration code in a game engine. Do not use for scene serialization (see scenes-memory) or build-time asset cooking (see asset-pipeline).
---

# Assets, Textures & I/O

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`; concrete paths and asset names are in `.agents/context/ProjectConfig.md`.

## Rules
- USE asynchronous background threads for all I/O operations and asset decoding. SEE `job-system`.
- USE unmanaged memory for image decoding (e.g. native `<ImageDecodeLib>`). FORBID loading assets into managed `byte[]`.
- USE Asset IDs or Handles (structs). FORBID passing raw resource pointers directly to gameplay logic.
- ENFORCE texture atlases and bindless textures to minimize state changes and pipeline binds. SEE `rendering-batching`.
- EXPOSE a loader that decodes a source path and returns a `TextureHandle` (null/fallback on missing or corrupt files). PUSH filtering through the upload call (`UploadTexture(..., TextureFilter)`): pixel art → `<TextureFilter.Nearest>` (crisp), smooth/large art → `<TextureFilter.Linear>`.

## Asset authorship
- ENCODE RGBA (not RGB): transparency is alpha-blended, so an entity quad shows only the opaque pixels of the image.
- AUTHOR entity sprites portrait/upright and bottom-center anchored to the tile (never square-cropped); AUTHOR tile art square, clipped to the tile shape at render (diamond in iso, square in top-down).

## Rules
- SEE `asset-pipeline` for build-time cooking/determinism, and the repo's concrete texture conventions (names, copy-to-output, placeholder generator) in `.agents/context/ProjectConfig.md`.