# Code Style

Code style follows the principles in `../../AGENTS.md` (`## Principles`):

- **SOLID**, **KISS**, **DRY**.
- Small, single-responsibility types and methods with clear names; prefer explicit ownership over hidden state.
- Keep it simple: the smallest solution that works; do not add speculative abstraction or generality.
- Don't repeat yourself: reuse existing code and contracts instead of duplicating logic; when a pattern appears twice, extract and share it.
- Code must be **easy to understand, refactor, and support**: follow existing project patterns and conventions.

The runtime hot-path rules that shape code style are in [`Coding.md`](Coding.md).
