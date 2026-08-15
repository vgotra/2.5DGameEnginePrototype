---
name: code-style
description: Applies clean-code conventions: SOLID, KISS, DRY, small single-responsibility types and methods, explicit ownership, file-scoped namespaces, the .editorconfig naming rules, and zero source comments. Use when writing or reviewing code in any project. Do not use for docs, tests, samples, or tooling that intentionally relax style rules.
---

# Code Style

Apply when writing or reviewing source code.

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- ENFORCE SOLID, KISS, and DRY.
- USE small, single-responsibility types and methods with clear names. PREFER explicit ownership over hidden state.
- KEEP it simple: the smallest solution that works. NEVER add speculative abstraction or generality.
- REUSE existing code and contracts instead of duplicating logic. EXTRACT and SHARE any pattern that appears twice.
- ENSURE code is easy to understand, refactor, and support. FOLLOW existing project patterns and conventions.
- APPLY the naming rules in `.editorconfig`: PascalCase types/members, camelCase locals/parameters, `_camel` private fields, `I` interface prefix.
- NAME variables, parameters, fields, and constants after what they represent or control. Avoid unclear abbreviations and single-letter names such as `x`, `v`, `p`, `t`, `a`, and `b` when a semantic name is available.
- EXTRACT magic numbers into descriptive constants such as `maxTitleScale`, `titleScaleDivisor`, `titleCenterRatio`, `minimumTitleWidth`, and `defaultTitleFontSize`.
- NAME boolean expressions after the condition they represent, and use multiline expressions when that makes intent clearer.
- USE file-scoped namespaces.
- NEVER add comments to source code; write code that is self-documenting.
- APPLY the runtime hot-path rules in the `coding-runtime`, `hot-path-interop`, and `memory-spans` skills when working in hot paths.

## Naming and Magic-Value Examples

```csharp
// Avoid unclear names and magic values
var x = Math.Min(viewportMaxWidth / 2, 320);

// Prefer names that explain the purpose
var titleScale = Math.Min(viewportMaxWidth / 2, maxTitleScale);
```

```csharp
// Avoid
var v = Math.Min(a / 2, b, c);

// Prefer
var titleScale = Math.Min(
    viewportMaxWidth / titleScaleDivisor,
    maxTitleScale,
    availableWidth);
```

```csharp
// Avoid
var p = width * 0.5f;

// Prefer
var centeredTitleX = viewportWidth * titleCenterRatio;
```

```csharp
// Avoid
if (x > 100 && y < 50)
{
    DoSomething();
}

// Prefer
var isTitleWithinBounds =
    titleWidth > minimumTitleWidth &&
    titleHeight < maximumTitleHeight;

if (isTitleWithinBounds)
{
    RenderTitle();
}
```

```csharp
// Avoid
var t = 16;

// Prefer
var defaultTitleFontSize = 16;
```
