---
name: resumable-context
description: Updates resumable-state convention files (a context directory and the docs index) after implementing and verifying a milestone, so the next session can resume without conversation history. Use at the end of a milestone whose code and verification are done. Do not use mid-implementation, or for one-off fixes that do not advance roadmap state.
---

# Resumable Context

Per the repo's resumable-state convention: read the context files before starting a milestone, update them after implementation + verification, so the next session can resume without conversation history.

After implementation + verification, in order:

1. New/renamed files or projects? Refresh the codebase structural index if one is used (e.g. a repo-graph MCP index — call its refresh tool).
2. New limitation or one resolved? Update the KnownIssues file.
3. Advance state in the CurrentState file — what is implemented, verification results, the next actions, risks.
4. Move a finished milestone into the Implemented file (shipped) and trim the Roadmap file (next actions).
5. Docs changed? Update the docs index entries.

## Style
- Terse bullets only — these files are auto-loaded into every session's instructions and are the session's working memory.
