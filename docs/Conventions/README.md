# Conventions

Coding, performance, packaging, and build conventions that differ from plain .NET defaults. Principles are in `../../AGENTS.md` (`## Principles`).

- [`Coding.md`](Coding.md) — runtime hot-path rules: no reflection/LINQ/allocations, structs/spans/explicit loops, allocation domains, parallelization, main-thread determinism.
- [`CodeStyle.md`](CodeStyle.md) — code style: SOLID/KISS/DRY, naming, ownership, simplicity, reusability.
- [`Packaging.md`](Packaging.md) — central package management and build policy.
- [`Restrictions.md`](Restrictions.md) — platform neutrality: what contracts and shared code must not contain.
- [`Commands.md`](Commands.md) — build, test, run, and shader-recompile commands.
