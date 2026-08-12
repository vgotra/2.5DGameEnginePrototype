---
name: ui-system
description: Applies UI conventions: retained vs immediate mode choice, widget hierarchy and layout, 9-slice scaling, input-driven widgets, and polled (not evented) updates. Use when writing or reviewing UI, menus, or HUD code in a 2D/2.5D engine. Do not use for text/fonts (see text-rendering) or gameplay input mapping (see input-action-mapping).
---

# UI System

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- CHOOSE retained or immediate mode deliberately; keep the choice consistent across the UI layer.
- MODEL widgets as a hierarchy with layout (anchors, docking, margins); compute layout once per change, not per frame.
- USE 9-slice scaling for panels and buttons to keep them resolution-independent.
- DRIVE widgets from the input action state (SEE `input-action-mapping`); UI consumes actions, it does not read devices directly.
- POLL widget state in systems; avoid OOP event hooks between widgets and gameplay. SEE `ecs`.
- RENDER UI as batches through the shared sprite/text path. SEE `rendering-batching`, `text-rendering`.
- KEEP steady-state UI updates zero-allocation: reuse widget state structs and layout buffers. SEE `object-pooling`.
