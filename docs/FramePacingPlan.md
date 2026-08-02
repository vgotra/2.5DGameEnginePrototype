# Frame Pacing Plan

Plan for roadmap item 1 — **Stable game loop and window lifecycle** (frame pacing / clean shutdown). This file records the diagnosis and the implementation. Steps 1-5 are **implemented** (2026-08-03); step 6 (sim-to-present interpolation) is deferred.

## Symptom

The sample runs a 60 Hz fixed-step simulation (`GameClock.FixedStep = 1/60`, `src/Engine.Core/GameClock.cs:5`) through the Vulkan path, and continuous camera pans visibly judder. The sim state is deterministic; the issue is *when* each frame reaches the screen.

## Root causes

The root causes below describe the **original** (pre-fix) implementation; the line numbers they referenced moved during the rework and are omitted.

1. **FIFO vsync on the swapchain.** The original `VulkanRenderer.CreateSwapchain` picked `presentMode = modes[0]`. On Windows drivers the first present mode is `VK_PRESENT_MODE_FIFO_KHR` (vsync on), so `vkQueuePresentKHR` is gated to vblank boundaries.
2. **Double buffering with a single in-flight fence.** The original swapchain used `minImageCount = max(capabilities.minImageCount, 2)` and `BeginFrame` waited the one `_inFlight` fence *before* acquiring. CPU and GPU never overlap, and the pipeline has no extra image to absorb timing jitter.
3. **Per-frame staging upload + `vkQueueWaitIdle` before present.** The original `BatchRenderer.EndFrame` copied the whole vertex/index set through a staging buffer, submitted, then called `vkQueueWaitIdle`. The CPU drained the entire queue before presenting, so per-frame cost was `upload + draw -> drain -> vblank wait -> Thread.Sleep(2)`; any jitter landed on the visible frame as a skipped/delayed beat.
4. **Coarse delta-time source.** The original sample loop used `Environment.TickCount64` (≈15.6 ms granularity on Windows) for dt. The `GameClock` accumulator absorbs the jitter into 0-2 fixed steps, but that jitter is then gated by the vsync+drain, compounding the irregularity.

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

- **Implemented (2026-08-03).** Steps 1-5 below landed in `Engine.Core`, `Engine.Rendering.Vulkan`, and `samples/IsometricSandbox`:
  1. **High-res timer + frame cap** — `Engine.Core.FrameTimer` (Stopwatch-based `Advance`/`WaitForNextFrame`); the sample replaces `Environment.TickCount64` + `Thread.Sleep(2)`, and `--cap <fps>` configures the target (default unpaced).
  2. **Present mode + buffering** — `CreateSwapchain` prefers `VK_PRESENT_MODE_MAILBOX_KHR` (fallback FIFO) and requests 3 images (clamped to `maxImageCount`).
  3. **Synchronization rework** — a 3-slot frame-in-flight pool (`_imageAvailable`/`_renderFinished`/`_fences`) plus a per-swapchain-image fence map (`_imagesInFlight`); `BeginFrame` waits only the current slot + acquired image, so the CPU overlaps `FramesInFlight` frames. The per-frame `vkQueueWaitIdle` in `BatchRenderer.EndFrame` is gone — staging→device copies are recorded into the main command buffer with a `TRANSFER → VERTEX_INPUT` barrier. `vkQueueWaitIdle` remains only in `Resize`, `Dispose`, and the rare buffer-growth path. `ErrorOutOfDateKHR` at acquire triggers a swapchain rebuild + re-acquire; at present it is non-fatal.
  4. **Persistent dirty-gated buffers** — vertex/index/staging buffers persist per frame slot; an FNV hash gate skips the staging write + copy when geometry is unchanged.
  5. **Clean shutdown** — ESC/window close already exit the loop; `Dispose` drains with `vkDeviceWaitIdle` and tears down the per-image sync objects.
- **Deferred:** step 6 (sim-to-present interpolation) — not needed for 60 Hz fixed-step at 60 Hz refresh; revisit if refresh-rate-independent smoothness is required.
- Verification: `dotnet build Engine.sln --nologo` 0 warnings, smoke tests pass, sample runs under default/`--2d`/`--fullscreen` and `--cap 60|80|100` with clean close.
