# Coding Conventions

Performance and allocation rules for runtime code.

- **No reflection, no LINQ, no managed allocation in runtime hot paths.** Use structs, spans, explicit loops, and preallocated buffers.
- Startup, frame, and persistent allocation domains are separate. Zero-GC is a **target for steady state**, not an unverified promise.
- Parallelize only asset decoding, shader compilation, uploads, and extraction for maps above roughly 10,000 visible tiles.
- Keep fixed-step player movement/collision on the main thread for deterministic ordering.
