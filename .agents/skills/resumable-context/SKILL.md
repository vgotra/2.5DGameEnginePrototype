---
name: resumable-context
description: Updates resumable-state convention files (a context directory and the docs index) after implementing and verifying a milestone, so the next session can resume without conversation history. Use at the end of a milestone whose code and verification are done. Do not use mid-implementation, or for one-off fixes that do not advance roadmap state.
---

# Resumable Context

Per the repo's resumable-state convention: read the context files before starting a milestone, update them after implementation + verification, so the next session can resume without conversation history. `CurrentState.md` is a concise present-state snapshot, `Implemented.md` is a brief shipped-feature inventory, and `CompletedMilestones.md` is the authoritative completed-milestone archive.

After implementation + verification, in order:

1. New/renamed files or projects? Refresh the codebase structural index if one is used (e.g. a repo-graph MCP index — call its refresh tool).
2. New limitation or one resolved? Update the KnownIssues file.
3. Advance `CurrentState.md` with present architecture, active constraints, next actions, and risks only.
4. Add a concise feature entry to `Implemented.md`; do not copy milestone history or verification logs there.
5. Add the completed milestone, scope, and verification summary to `CompletedMilestones.md`; do not duplicate the feature inventory there.
6. Trim `Roadmap.md` and update docs index entries when documentation structure changes.

## File Roles

- `CurrentState.md`: current snapshot only; no historical milestone archive.
- `Implemented.md`: brief feature inventory grouped by subsystem; no detailed verification history.
- `CompletedMilestones.md`: one authoritative entry per completed milestone with concise scope and verification.

## Style
- Terse bullets only — these files are auto-loaded into every session's instructions and are the session's working memory.
