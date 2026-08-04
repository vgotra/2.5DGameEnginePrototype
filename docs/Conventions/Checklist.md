# Pre-Generation AI Checklist

1. Are there zero hidden heap allocations (`new`, boxing, closures, strings)? MUST BE YES.
2. Is branchless logic prioritized over `if/else` in loops? MUST BE YES.
3. Are all native buffers explicitly freed? MUST BE YES.
4. Are ECS systems processing raw pointers/spans instead of objects? MUST BE YES.
5. Is multi-threading avoiding `lock` and respecting exclusive write access? MUST BE YES.
6. Are physics and assets relying on Handles/IDs rather than managed references? MUST BE YES.
