# Coding Conventions

- ENFORCE zero reflection, LINQ, and managed allocation in runtime hot paths. USE structs, spans, explicit loops, and preallocated buffers.
- SEPARATE startup, frame, and persistent allocation domains. TREAT zero-GC as a steady-state target, not an unverified promise.
- PARALLELIZE only asset decoding, shader compilation, uploads, and extraction for maps above roughly 10,000 visible tiles.
- KEEP fixed-step player movement and collision on the main thread for deterministic ordering.
- FORBID exceptions in hot paths and fixed-step simulation. USE return codes (`VkResult`, `bool`). THROW only at subsystem boundaries (setup, validation, unrecoverable).
