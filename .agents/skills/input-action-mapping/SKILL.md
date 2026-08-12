---
name: input-action-mapping
description: Applies input conventions: abstracting devices (keyboard/mouse/gamepad/touch) behind actions and action maps, sampling once per frame, deterministic consumption in the fixed step, and rebinding. Use when writing or reviewing input handling code in a game engine. Do not use for UI widget logic (see ui-system) or device-specific drivers.
---

# Input & Action Mapping

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ABSTRACT all devices (keyboard, mouse, gamepad, touch) behind named actions (e.g. Move, Jump) and context action maps (menu vs gameplay). Gameplay code never reads device state directly.
- SAMPLE devices once per frame into an input buffer; CONSUME the buffered actions deterministically in the fixed-step update. SEE `determinism`.
- SUPPORT touch gestures (tap, swipe, pinch) — the engine is mobile-first.
- STORE bindings data-driven (rebindable), not hard-coded in gameplay logic.
- KEEP input sampling off the fixed-step thread; the sim reads only the buffer.
- HANDLE window focus/occlusion: drop stale input when the app loses focus.
