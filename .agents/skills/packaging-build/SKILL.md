---
name: packaging-build
description: Applies packaging and build policy: central package management, version-less package references, and shared global build properties across projects. Use when adding packages, editing project/build files, or creating new projects. Do not use for application code, docs, or tests.
---

# Packaging and Build Policy

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE central package management. ADD new packages to the central package props file, referenced without a version.
- APPLY the global build properties in the shared build props file as configured for this repo: target framework, language version, nullable, implicit usings, unsafe blocks, and invariant globalization.
