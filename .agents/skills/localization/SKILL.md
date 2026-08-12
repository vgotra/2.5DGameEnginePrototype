---
name: localization
description: Applies localization conventions: string tables, locale selection and fallback, per-locale font/formatting handling, and zero-allocation lookups. Use when writing or reviewing localization, i18n, or multilingual text code in a game or app. Do not use for text rendering itself (see text-rendering) or for save/settings persistence (see persistence-config).
---

# Localization (i18n)

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- STORE strings in locale tables keyed by stable IDs; gameplay code references IDs, never inline user-facing text.
- LOOK UP strings with zero allocation in the hot path (ID → table, span return). SEE `hot-path-interop`.
- FALL BACK through a locale chain (exact → region → default) deterministically.
- HANDLE per-locale formatting (numbers, dates, plurals) explicitly; never assume the source locale's rules.
- ENSURE fonts cover the target script; fall back to a default font per script. SEE `text-rendering`.
- LOAD locale data through the asset system as cooked tables. SEE `assets-io`, `asset-pipeline`.
