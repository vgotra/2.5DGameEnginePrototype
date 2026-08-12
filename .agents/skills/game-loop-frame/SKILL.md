---
name: game-loop-frame
description: Applies game-loop and frame-pipeline conventions: fixed-step simulation with render interpolation, deterministic system execution order, frame pacing/cap and vsync, and main-thread vs worker-thread duties. Use when writing or reviewing the game loop, frame timing, or system scheduling code in a game engine. Do not use for fixed-step determinism rules alone (see determinism) or job scheduling (see job-system).
---

# Game Loop & Frame Pipeline

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- RUN the simulation at a fixed step driven by the clock accumulator; NEVER use per-frame wall-clock `dt` for simulation. SEE `determinism`.
- INTERPOLATE render state between the last two fixed steps for smooth visuals at any frame rate; never render at the raw fixed step.
- DEFINE a deterministic system execution order per fixed step and keep it stable across runs.
- CAP/pace the frame rate explicitly (e.g. a `<cap>` flag) and manage vsync deliberately; pacing is presentation, not simulation.
- KEEP the loop boundaries clear: fixed-step simulation, queue submit/present, and renderer-owned state on the main thread; workers handle decode/parallel work. SEE `job-system`.
- ENSURE frame timing and the fixed-step accumulator are measured with a monotonic clock, never wall-clock time. SEE `determinism`.
