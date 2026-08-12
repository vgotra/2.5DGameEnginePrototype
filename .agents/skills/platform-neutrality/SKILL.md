---
name: platform-neutrality
description: Keeps contracts and shared code OS-agnostic by forbidding OS-specific P/Invoke and types there, and confining OS-specific code to the platform backend projects behind platform seams (window, input, graphics surface). Use when writing or reviewing contracts, shared code, or platform backends. Do not use for single-platform-only backend code.
---

# Restrictions (Platform Neutrality)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`; concrete seams are in `.agents/context/ProjectConfig.md`.

## Rules
- FORBID OS-specific P/Invoke and types in contracts and shared code. Contracts stay the only boundary gameplay code sees.
- KEEP OS-specific code in the platform backend projects (`<ProjectName>.Platform.*`).
- CONSUME the platform-neutral native surface contract from the renderer.
- RECEIVE the OS surface through a platform-provided surface factory: the factory returns the required instance extensions and creates/destroys the surface as opaque `nint` handles. The renderer NEVER sees OS window structs (HWND/XID/NSView).
- PERFORM backend selection in the desktop platform entry point; the sample/app never references a concrete platform.

## Conventions
- Backends are chosen at RUNTIME (window creation), so cross-OS support is verification of shared code, not per-OS game logic.