# Recovery and Context

The resumable-state convention: every milestone reads `.agents/context/` before starting and updates it (plus `RELEASE_NOTES.md`) after implementation and verification, so the next session can resume without conversation history.

## What each context file holds

- **`CurrentState.md`** — what is implemented right now (backend contracts, Vulkan path, windowing), improvement sets, verification results, the next three actions, and risks.
- **`KnownIssues.md`** — current known issues and limitations with causes and status (e.g. the `--cap` busy-wait thread).

There is no structure-map file; use the `mcp-repo-graph` MCP server for the codebase structural map.

## When to read and update

- **Before** a milestone: read `docs/README.md` (docs index), the relevant topic docs, and the other context files.
- **After** implementation + verification: update the context files and add a dated entry to `RELEASE_NOTES.md`.

## Milestone checklist

1. New/renamed files or projects? Refresh the `mcp-repo-graph` index.
2. New limitation or one resolved? Update `KnownIssues.md`.
3. Advance the state — `CurrentState.md` (implemented + verification + next three actions).
4. Docs changed? Update `docs/README.md` index entries.

## How it loads

`opencode.json` → `instructions` auto-loads `.agents/context/*.md` into every session, so these files are always in-context. Keep them concise and current — they are the session's working memory.
