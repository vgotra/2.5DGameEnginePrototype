---
name: swapchain-lifecycle
description: Applies graphics-API swapchain lifecycle conventions: creation, acquire→draw→present flow with per-frame synchronization, and resize/fullscreen recreation. Use when writing or reviewing swapchain, presentation-mode, or VSync/pacing code in a graphics project. Do not use for frame-loop pacing (see game-loop-frame) or window/input backends (see platform-neutrality).
---

# Swapchain Lifecycle

## Apply
- Substitute every `<...>` placeholder with this repo's actual names from `.agents/context/ProjectConfig.md` before applying.

## Rules
- CREATE the swapchain from the surface at startup: query surface capabilities to pick a color format and `<PresentMode>` (e.g. MAILBOX for low-latency, FIFO for vsync); size the image count to the returned image capacity, never assume a fixed count.
- BIND framebuffers to swapchain images at creation (one framebuffer per image index); rebuild them together with the swapchain on any size change.
- FRAME flow is acquire → draw → present: acquire the image, record the frame into its command buffer (layoutTransitions to the attach layout, draw, transition to present layout), then present. Sync per-frame with fences/semaphores so the CPU never writes into an image that is still in use by the GPU.
- RECREATE on resize/fullscreen (and on surface-loss/suboptimal): drain GPU work first (e.g. `vkQueueWaitIdle` — accept the stall only here, not per frame), destroy swapchain + framebuffers, recreate, and rebuild any resources coupled to swapchain count. Array/cache the structure growing out of image-count (e.g. per-frame command pools) so recreation is uniform.
- NEVER record copies into a render pass; do geometry copies and layout barriers BEFORE the pass (a copy-inside-a-render-pass is a VUID violation), with the pass begun after uploads/barriers.
- USE a non-blocking present mode only with deliberate pacing: MAILBOX presents every completed frame, so an unpaced CPU renders and presents as fast as it can — add a frame cap/pace for a steady cadence. SEE `game-loop-frame`.
- KEEP the present path allocation-free per frame: reuse per-swapchain images, command buffers, and sync objects. SEE `profiling-diagnostics`.
- TEST the fullscreen-toggle and drag-resize paths (both rebuild the swapchain) and verify no stale images or pools are used after recreation. SEE `build-and-verify`.
