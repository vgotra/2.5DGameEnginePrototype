---
name: text-rendering
description: Applies text rendering conventions: glyph atlases, font metrics, SDF fonts for scaling, cached glyph quads, and batch-friendly text. Use when writing or reviewing text, font, or UI-text code in a 2D/2.5D engine. Do not use for general sprite batching (see rendering-batching) or UI layout (see ui-system).
---

# Text Rendering

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- RENDER text from a single glyph atlas texture; glyphs are packed once, not per draw.
- STORE font metrics (ascent, descent, line height, kerning) in a value-type table; no allocations when measuring or laying out.
- USE SDF (signed-distance-field) fonts for crisp scaling with minimal memory.
- CACHE glyph quads so unchanged text reuses cached geometry; rebuild only on string change.
- BATCH text through the same sprite path as other UI. SEE `rendering-batching`.
- HANDLE Unicode/UTF-8 without per-character managed strings in the hot path. SEE `hot-path-interop`.
