---
name: performance-budget
description: Applies per-frame performance budgeting: time budgets per system, mobile 30/60fps targets, and adaptive resolution/quality scaling. Use when writing or reviewing frame-time management, adaptive quality, or mobile performance code. Do not use for measuring metrics (see profiling-diagnostics) or the external benchmark gate (see build-and-verify).
---

# Performance Budget

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- SET a frame-time budget for the target (e.g. 16.6 ms at 60 fps, 33.3 ms at 30 fps on mobile) and allocate sub-budgets per system.
- TARGET the steady state from `profiling-diagnostics` (frame time, allocation) — budgets are enforced against measured data, not guesses.
- SCALE adaptively under pressure: drop quality tiers, reduce resolution/particles/LOD before frame time overruns. SEE `particles-effects`, `publish-native-mobile`.
- PRIORITIZE the fixed-step sim and input budget; presentation work absorbs the remaining headroom. SEE `game-loop-frame`.
- GATE perf verdicts on same-machine measurements only. SEE `build-and-verify`.
