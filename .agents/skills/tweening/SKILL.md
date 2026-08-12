---
name: tweening
description: Applies tween and easing conventions: pooled tween objects, easing curves, fixed-step-driven updates, and zero-allocation completion. Use when writing or reviewing animation, UI motion, or gameplay transition code in a game engine. Do not use for sprite/frame animation (see animations-2d) or object lifecycle pooling in general (see object-pooling).
---

# Tweening & Easing

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- RUN tweens off the fixed-step accumulator, never wall-clock time. SEE `determinism`.
- POOL tween objects at fixed capacity; acquire/release instead of allocating per tween. SEE `object-pooling`.
- IMPLEMENT easing as a small library of pure functions (inlined, branchless where possible). SEE `math`.
- STORE tween state in value-type structs; completion callbacks must not close over allocations.
- KEEP tween updates zero-allocation in steady state. SEE `pre-generation-checklist`.
