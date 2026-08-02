# Performance Budget

The current MVP uses a 20x20 map with viewport culling in render extraction. Runtime input uses a bitmask with no per-frame array copy.

The Vulkan path keeps persistent per-frame-slot vertex/index/staging buffers and records staging→device copies into the main command buffer behind a `TRANSFER → VERTEX_INPUT` barrier, issuing a single indexed draw; an FNV dirty gate skips the upload when geometry is unchanged, so there is no per-frame `vkQueueWaitIdle` (it remains only for resize, dispose, and rare buffer growth). Frame delivery uses `VK_PRESENT_MODE_MAILBOX_KHR` (fallback FIFO) with triple buffering, a 3-slot frame-in-flight pool, and per-swapchain-image fences, so the CPU overlaps frames; the loop is unpaced by default with an optional `--cap` frame cap. Details in `docs/FramePacingPlan.md`.

Multithreading policy:

- Keep fixed-step player movement and collision on the main simulation thread for deterministic ordering.
- Parallelize asset decoding, shader compilation, resource uploads, and large-map render extraction.
- Do not schedule jobs for a 20x20 tile loop; job overhead is larger than the work.
- For maps above roughly 10,000 visible tiles, partition extraction into row ranges and use the job system, writing into disjoint output ranges.
- Keep Vulkan command recording on the owning render thread unless command pools are explicitly separated per worker.
- Use worker threads for background loading and hot reload, then publish completed immutable resources at a frame boundary.
