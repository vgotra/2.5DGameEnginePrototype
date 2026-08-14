# Planned Milestones

This file contains future work only. Verified implementation history is recorded in `Implemented.md` and `CompletedMilestones.md`.

## Historical milestones 15–19

Milestones 15–19 are complete and remain documented in `CompletedMilestones.md`; their former plans are intentionally not duplicated here.

## Milestone 20 — Material System and Renderer Profiling

### Goal

Implement renderer-neutral material handles and independent shader parameters while preserving indexed textures and blend ordering.

### Work

- Add renderer-owned material resources and explicit shader parameters.
- Keep material selection renderer-neutral and allocation-free in sprite hot paths.
- Measure material state changes with the focused benchmark catalog.

### Acceptance

- Alpha/additive behavior, painter order, and indexed texture sampling remain unchanged.
- Focused renderer tests and benchmarks cover material selection and allocations.

### Dependencies and verification

Build the solution, run the plain console tests, run bounded sample smoke checks, run the Release benchmark gate, and search for direct gameplay ECS construction.

## Historical — Milestone 16: Runtime API and Code Simplification

### Goal

Remove duplicate paths and unnecessary wrappers across `Game`, `World`, `Scene`, spawning, `EntityCommands`, and sample integration.

### Acceptance

- Public APIs expose game-facing concepts while engine systems retain appropriate sparse ECS access.
- Lifecycle and ownership rules remain explicit.
- Obsolete abstractions are removed only after repository-wide reference checks.
- Existing behavior, deterministic ordering, and measured performance remain valid.

### Dependencies and verification

Run the full build-and-verify workflow, including tests, bounded samples, Release benchmarks, allocation checks, and API searches.

## Historical — Milestone 17: Simplified Configurable Console Tests

### Goal

Make the plain console test application easy to run in full or focused modes without introducing a test framework.

### Work

- Run all suites by default.
- Add suite and individual-case selection.
- Add optional stop-on-first-failure behavior.
- Provide clear summaries and stable exit codes.
- Preserve deterministic configuration and existing ECS, scheduler, allocation, gameplay, rendering, and runtime-contract coverage.

### Acceptance

- Existing no-argument verification behavior remains compatible.
- Focused runs execute only the requested cases.
- Failures are actionable and process status is reliable.

### Dependencies and verification

Keep test definitions independent from command-line parsing. Verify default and focused runs, invalid arguments, failure exit behavior, and the repository’s normal smoke-test command.

## Historical — Milestone 18: Simplified Configurable Benchmarks

### Goal

Make benchmark selection and workload configuration explicit while preserving comparison and allocation gates.

### Work

- Select benchmark groups or individual cases.
- Configure iterations, warm-up behavior, workload sizes, and execution modes where supported.
- Keep benchmark definitions independent from command-line parsing.
- Preserve machine-readable output, baseline comparison, allocation tolerance, deterministic setup, and resource disposal.

### Acceptance

- Existing Release benchmark verification remains compatible.
- Focused benchmark runs are faster and clearly identified.
- Configured workloads produce reproducible results.

### Dependencies and verification

Depends on the existing benchmark comparison contract and may follow Milestone 17. Verify default, focused, configured, machine-readable, comparison, and invalid-argument runs.

## Historical — Milestone 19: Documentation and Verification Reconciliation

### Goal

Synchronize architecture, roadmap, implementation inventory, milestone history, and workflow state after Milestones 15–18.

### Acceptance

- `Simplified Architecture v2.md` contains implemented architecture and current constraints only.
- This file contains only remaining work.
- `Implemented.md`, `Roadmap.md`, `CompletedMilestones.md`, and workflow logs agree.
- Repository-wide searches find no obsolete API terminology.
- Full build-and-verify passes: solution build, plain console tests, bounded sample runs, Release benchmarks, and diff validation.

### Dependency

Run after Milestones 15–18, or whenever a substantial API/test/benchmark simplification changes the documented contracts.

## Milestone 20 — Material System and Renderer Profiling

### Goal

Replace texture-backed material selection with explicit material resources and use profiling to guide further renderer decomposition.

### Acceptance

- Material shader parameters are represented by explicit renderer-owned resources.
- Material and blend state changes are batched and measured.
- Further Vulkan extraction is justified by profiling data or explicitly deferred.
- Release benchmark and bounded Vulkan verification remain clean.
