---
name: milestone-context
description: Updates the resumable-state convention files (.agents/context/*, RELEASE_NOTES.md, docs index) after implementing and verifying a milestone. Use at the end of a milestone whose code and verification are done. Do not use mid-implementation, or for one-off fixes that do not advance roadmap state.
---

## What I do

Per `docs/RecoveryAndContext.md` (the authoritative convention): read `.agents/context/` before starting a milestone, update it after implementation + verification, so the next session can resume without conversation history.

After implementation + verification, in order:

1. New/renamed files or projects? Refresh the `mcp-repo-graph` index (call `refresh`).
2. New limitation or one resolved? Update `.agents/context/KnownIssues.md`.
3. Advance state: `.agents/context/CurrentState.md` — what is implemented, verification results, the next three actions, risks.
4. Docs changed? Update `docs/README.md` index entries.
5. Add a dated entry to `RELEASE_NOTES.md`.

## Style

- Terse bullets only — these files are auto-loaded into every session (`opencode.json` instructions) and are the session's working memory.
- There is no structure-map file; the structural map comes from `mcp-repo-graph`, not a file.
