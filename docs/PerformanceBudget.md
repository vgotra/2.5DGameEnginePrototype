# Performance Budget

The current MVP uses a 20x20 map, cached GDI backbuffer, cached brushes, reusable polygon storage, dirty rendering, and viewport culling. Runtime input uses a bitmask with no per-frame array copy.

Multithreading policy:

- Keep fixed-step player movement and collision on the main simulation thread for deterministic ordering.
- Parallelize asset decoding, shader compilation, resource uploads, and large-map render extraction.
- Do not schedule jobs for a 20x20 tile loop; job overhead is larger than the work.
- For maps above roughly 10,000 visible tiles, partition extraction into row ranges and use the job system, writing into disjoint output ranges.
- Keep Vulkan command recording on the owning render thread unless command pools are explicitly separated per worker.
- Use worker threads for background loading and hot reload, then publish completed immutable resources at a frame boundary.
