# AI Implementation Workflow

This document defines the mandatory execution protocol for AI agents performing multi-step implementation work in this repository.

The purpose is to preserve implementation state across context loss, agent restarts, compaction, and separate coding sessions while keeping the process simple and human-readable.

## 1. Workflow State

All implementation state is stored in:

`.ai_workflow_logs/`

The directory is local-only and MUST be git-ignored.

It contains:

- `current_milestone.md` — pending work for the active milestone or plan.
- `in_progress.md` — the item currently being implemented.
- `completed_items.md` — verified completed work, newest first.

These files represent operational execution state, not retrospective documentation.

They MUST reflect the actual repository state.


## 2. State Machine

Every implementation item follows this lifecycle:

PENDING  
`current_milestone.md`

→

IN PROGRESS  
`in_progress.md`

→

VERIFIED COMPLETE  
`completed_items.md`

An implementation item MUST NOT skip the IN PROGRESS state.

An item MUST NOT enter VERIFIED COMPLETE until its Definition of Done has been satisfied.


## 3. Mandatory Session Startup

Before modifying code for any multi-step implementation task:

1. Read `.ai_workflow_logs/in_progress.md`.
2. Read `.ai_workflow_logs/current_milestone.md`.
3. Consult `.ai_workflow_logs/completed_items.md` when necessary to determine whether work has already been completed.
4. Inspect the relevant repository state.
5. Reconstruct the current execution state.
6. Resume the existing in-progress item before selecting new work.

Do NOT begin implementation before this startup procedure has been performed.

If `.ai_workflow_logs/` or one of its required files does not exist, initialize it before implementation.

If `in_progress.md` contains active work, continue that work unless:

- it is blocked;
- repository state proves it is obsolete;
- its prerequisite is missing;
- the user explicitly changes priority.

Record the reason before abandoning or replacing an active item.


## 4. Starting a New Milestone or Plan

Before making the first implementation change for a new multi-step plan:

1. Determine the concrete implementation steps.
2. Check `completed_items.md` to avoid duplicating verified work.
3. Write the active plan to `current_milestone.md`.
4. Use one concrete, independently verifiable checklist item per entry.
5. Record milestone metadata.

Example:

# Current Milestone

Name: Frame Lifecycle Simplification
Status: Active
Started: 2026-08-12

## Pending

- [ ] Implement FrameContext lifecycle
- [ ] Add per-frame command pools
- [ ] Add swapchain recreation handling
- [ ] Verify frame synchronization
- [ ] Remove obsolete frame-management abstractions

## Notes

Keep swapchain ownership inside the rendering/platform boundary.
Do not expose Vulkan presentation details to Game, World, or Scene.


## 5. Starting an Item

Before modifying code for a checklist item:

1. Select the next concrete pending item.
2. Remove it from the pending section of `current_milestone.md`.
3. Write it to `in_progress.md`.
4. Record:
   - start date;
   - acceptance criteria;
   - relevant context;
   - architectural decisions;
   - constraints;
   - blockers, if any.
5. Only then begin implementation.

Normally, exactly one implementation item should be active.

Example:

# In Progress

## Item

Implement swapchain recreation.

## Started

2026-08-12

## Acceptance Criteria

- Window resize recreates the swapchain.
- Old swapchain resources are released correctly.
- Rendering continues after recreation.
- No Vulkan validation errors are introduced.
- Swapchain implementation details remain inside the rendering/platform layer.

## Notes

- Preserve the existing FrameContext abstraction.
- Do not introduce a new general-purpose abstraction unless required.
- Prefer the smallest implementation satisfying the acceptance criteria.

## Verification

Not yet verified.


## 6. During Implementation

Keep `in_progress.md` synchronized with meaningful discoveries.

Update `Notes` when implementation reveals:

- an architectural constraint;
- an unexpected dependency;
- a blocker;
- an important design decision;
- a deviation from the original plan;
- follow-up work that should become another checklist item.

Do NOT use the workflow log as a verbose activity diary.

Record decisions and state, not every command or code edit.


## 7. Completing an Item

Code being written does NOT mean an item is complete.

Before completing an item:

1. Review its acceptance criteria.
2. Build the affected project.
3. Run relevant tests.
4. Run any appropriate static analysis or validation.
5. Inspect the resulting implementation for temporary/debug code.
6. Verify that the implementation matches the intended architecture.

If verification succeeds:

1. Remove the active item from `in_progress.md`.
2. Add it to the TOP of `completed_items.md`.
3. Add a concise verification note.
4. Update `current_milestone.md`.
5. Select the next item only after state synchronization is complete.

If verification fails:

1. Keep the item in `in_progress.md`.
2. Record the failure under `Notes` or `Verification`.
3. Continue working on the same item.

Never mark failed, partial, assumed, or unverified work as completed.


## 8. Definition of Done

An implementation item is complete only when all applicable conditions are satisfied:

- implementation is complete;
- acceptance criteria are satisfied;
- affected projects build successfully;
- relevant tests pass;
- no new compiler errors are introduced;
- no unintended compiler warnings are introduced;
- applicable analyzers/validation pass;
- temporary/debug code has been removed;
- obsolete code made unnecessary by the change has been removed when safe;
- implementation matches repository architecture and conventions.

If any required condition is not satisfied, the item remains IN PROGRESS.


## 9. Completed Items

`completed_items.md` stores verified implementation history.

Newest items MUST appear first.

Example:

# Completed Items

## 2026-08-12 — Implement FrameContext lifecycle

Verified:
- solution builds successfully;
- frame lifecycle tests pass;
- Vulkan validation reports no new errors.

## 2026-08-11 — Simplify entity query API

Verified:
- ECS tests pass;
- benchmark sample runs successfully.

Do not move an item back into implementation merely because context was lost.

Repository evidence or an explicit user request is required before reopening verified work.


## 10. Context Loss and Recovery

After:

- a new agent session;
- context compaction;
- context loss;
- interrupted implementation;
- resuming repository work;

the agent MUST reconstruct state from the workflow files.

Recovery order:

1. Read `in_progress.md` FIRST.
2. Read `current_milestone.md` SECOND.
3. Consult `completed_items.md`.
4. Inspect relevant repository changes.
5. Continue the active item.

Never assume that missing conversational context means implementation must restart.

Never redo work already recorded as verified in `completed_items.md`.

The repository and workflow files are the source of truth.


## 11. Continuous Synchronization

Workflow state MUST be updated when execution state changes.

Do not wait until the end of the session.

Required sequence:

Select item
→ update workflow state
→ implement
→ verify
→ update workflow state
→ select next item

The following is NOT acceptable:

Implement several items
→ update all workflow files afterward

The logs exist specifically to survive interruption between implementation steps.


## 12. Blocked Work

If an item cannot proceed:

1. Keep the item recorded in `in_progress.md`.
2. Set its status to `Blocked`.
3. Record the exact blocker.
4. Record what is required to unblock it.

Do not silently skip blocked work.

Another independent item may be selected only when doing so does not violate dependencies.


## 13. Scope Changes

If the user changes the active plan:

1. Preserve already verified work.
2. Record the change in the active milestone Notes.
3. Update pending items.
4. Update or replace the in-progress item when necessary.
5. Do not discard useful implementation history.

Explicit user instructions override the current milestone.


## 14. Simplicity Rule

Do not introduce architecture merely because it may be useful later.

For every implementation item:

- prefer the smallest design satisfying current requirements;
- reuse existing abstractions where appropriate;
- avoid speculative generalization;
- avoid unnecessary wrappers and indirection;
- remove obsolete complexity when the milestone explicitly replaces it.

Future hypothetical requirements are not sufficient justification for additional architecture.


## 15. End of Milestone

A milestone is complete when:

- no pending items remain;
- no item remains in progress;
- all required items are present in `completed_items.md`;
- milestone-level verification succeeds.

Update `current_milestone.md`:

Status: Completed

Do not automatically begin another roadmap milestone unless it is already approved for implementation.
