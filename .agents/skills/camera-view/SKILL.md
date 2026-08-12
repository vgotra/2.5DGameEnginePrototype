---
name: camera-view
description: Applies camera and view conventions: orthographic projection, world-to-screen transforms, camera follow/smoothing, shake, zoom, and viewport. Use when writing or reviewing camera, view-projection, or screen-space conversion code in a 2D/2.5D engine. Do not use for renderer batching (see rendering-batching) or simulation determinism (see determinism).
---

# Camera & View

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- USE an orthographic projection matched to the pixel/viewport scale for 2D/2.5D rendering.
- KEEP one canonical view matrix per frame; derive world-to-screen and screen-to-world conversions from it. NEVER maintain duplicate transform state.
- IMPLEMENT camera follow with smoothing (e.g. damped towards target) in the fixed-step update, not per-render-frame.
- SUPPORT shake and zoom as additive, deterministic offsets composed onto the canonical view.
- USE the camera bounds for culling: visible-set and chunk visibility derive from the view frustum/bounds. SEE `culling-spatial`, `tilemap-rendering`.
- STORE camera state in a value-type struct; systems take it by `in`/`ref`. SEE `hot-path-interop`.
