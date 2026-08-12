---
name: pre-generation-checklist
description: Verifies new or modified runtime code against a pre-generation checklist: zero hidden allocations, branchless loops, freed native buffers, span/pointer processing, lock-free threading, handle-based resources, zero comments. Use before generating or finalizing runtime code. Do not use for docs, tests, tools, or samples that intentionally relax these rules.
---

# Pre-Generation AI Checklist

Run before finalizing runtime code. All MUST BE YES:

1. Are there zero hidden heap allocations (`new`, boxing, closures, strings)? MUST BE YES.
2. Is branchless logic prioritized over `if/else` in loops? MUST BE YES.
3. Are all native buffers explicitly freed? MUST BE YES.
4. Are systems processing raw pointers/spans instead of objects? MUST BE YES.
5. Is multi-threading avoiding `lock` and respecting exclusive write access? MUST BE YES.
6. Are physics and assets relying on Handles/IDs rather than managed references? MUST BE YES.
7. Are there zero comments in source code? MUST BE YES.
