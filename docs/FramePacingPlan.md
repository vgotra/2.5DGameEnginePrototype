# Frame Pacing Plan

Plan for roadmap item 1 — **Stable game loop and window lifecycle** (frame pacing / clean shutdown). This file records the diagnosis and the planned approach; **nothing here is implemented yet**. The current frame-delivery limitation is tracked in `.agents/context/KnownIssues.md` and `RenderingDesign.md`.

## Symptom

The sample runs a 60 Hz fixed-step simulation (`GameClock.FixedStep = 1/60`, `src/Engine.Core/GameClock.cs:5`) through the Vulkan path, and continuous camera pans visibly judder. The sim state is deterministic; the issue is *when* each frame reaches the screen.

## Root causes

1. **FIFO vsync on the swapchain.** `VulkanRenderer.CreateSwapchain` picks `presentMode = modes[0]` (`src/Engine.Rendering.Vulkan/VulkanRenderer.cs:275`). On Windows drivers the first present mode is `VK_PRESENT_MODE_FIFO_KHR` (vsync on), so `vkQueuePresentKHR` is gated to vblank boundaries.
2. **Double buffering with a single in-flight fence.** `minImageCount = max(capabilities.minImageCount, 2)` (`VulkanRenderer.cs:261`) and `BeginFrame` waits the one `_inFlight` fence *before* acquiring (`VulkanRenderer.cs:116-119`). CPU and GPU never overlap, and the pipeline has no extra image to absorb timing jitter.
3. **Per-frame staging upload + `vkQueueWaitIdle` before present.** `BatchRenderer.EndFrame` copies the whole vertex/index set through a staging buffer, submits, then `vkQueueWaitIdle` (`src/Engine.Rendering.Vulkan/BatchRenderer.cs:214-215`). The CPU drains the entire queue before presenting, so per-frame cost is `upload + draw -> drain -> vblank wait -> Thread.Sleep(2)`; any jitter lands on the visible frame as a skipped/delayed beat.
4. **Coarse delta-time source.** The sample loop uses `Environment.TickCount64` (≈15.6 ms granularity on Windows) for dt (`samples/IsometricSandbox/Program.cs:51-52`). The `GameClock` accumulator absorbs the jitter into 0-2 fixed steps, but that jitter is then gated by the vsync+drain, compounding the irregularity.

## Planned approach

Tracked under roadmap item 1. Implementation order:

1. **High-resolution timer.** Replace `Environment.TickCount64` dt with a `Stopwatch`-based clock in the sample loop (or a shared `GameLoop` host), and add a configurable frame cap / vsync policy.
2. **Swapchain present mode + buffering.** Prefer `VK_PRESENT_MODE_MAILBOX_KHR` (tear-free, non-blocking) with a fallback to `VK_PRESENT_MODE_FIFO_KHR`; request 3 swapchain images (triple buffering) when the driver allows.
3. **Synchronization rework.** Replace the single `_inFlight` fence with a fence per swapchain image; `BeginFrame` waits only on the acquired image's fence. Remove the per-frame `vkQueueWaitIdle` in `BatchRenderer.EndFrame`; keep `vkDeviceWaitIdle` only for resize/realloc.
4. **Persistent buffers.** Keep vertex/index buffers allocated persistently and re-upload only when contents change (dirty flag), with a small staging-buffer pool instead of one fresh upload per frame.
5. **Clean shutdown.** ESC / window close stops the loop, then `vkDeviceWaitIdle` and orderly teardown (extend the existing `Dispose` path).
6. **Optional: sim-to-present interpolation.** Interpolate between fixed-step states at present time for refresh-rate-independent smoothness.

## Out of scope

- Texture sampling, asset loading, and other roadmap items — see `docs/Roadmap.md`.

## Status

- **Not implemented.** This is a planning record; the code still matches the "Current limitations" sections of `RenderingDesign.md`.
- Next session: implement steps 1-5 (and optionally 6) in the sample + `Engine.Rendering.Vulkan`, then verify smoothness directly on the sample.
