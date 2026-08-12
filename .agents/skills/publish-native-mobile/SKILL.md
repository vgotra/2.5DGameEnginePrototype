---
name: publish-native-mobile
description: Applies mobile-first publish and AOT conventions: NativeAOT/trimming-safe code, package-size budgets, asset packaging, and platform publish profiles, keeping OS code behind platform seams. Use when preparing builds, publishing, or reviewing AOT/trimming-safety for a mobile or performance-sensitive project. Do not use for dev-loop builds (see build-and-verify) or general platform abstraction (see platform-neutrality).
---

# Native & Mobile Publish

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- WRITE NativeAOT/trimming-safe code: no reflection, dynamic loading, or codegen at runtime. SEE `coding-runtime`, `hot-path-interop`.
- KEEP OS-specific code behind the platform seams; the shared codebase stays AOT-clean. SEE `platform-neutrality`.
- SET explicit package-size budgets per target and gate publishes on them.
- PACKAGE assets for the target platform (per-format textures, compression); never ship dev-only assets.
- USE per-platform publish profiles with trimming, ready-to-run, and minification settings as configured for the project.
- VERIFY the published app boots and runs the full verification loop. SEE `build-and-verify`.
