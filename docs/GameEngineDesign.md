# Game Engine Design

## Goals

The engine is a Windows-first .NET 10 runtime for 2D/2.5D isometric games. It uses Vortice.Vulkan for explicit GPU control and keeps gameplay-facing contracts independent from platform, audio, and physics backends. Linux (X11/Wayland via SDL2) and macOS are planned targets; platform seams (`IGameWindow`, `IInputState`, `NativeWindowSurface`, `GamePlatform`) are in place today (see `docs/LinuxSupportPlan.md`).

## Frame lifecycle

Input sampling -> fixed-step simulation -> physics -> deferred ECS changes -> render extraction -> asset uploads -> Vulkan submission -> audio submission -> presentation. Each phase has an explicit synchronization boundary. The fixed simulation step is 60 Hz with bounded catch-up. Frame delivery uses `VK_PRESENT_MODE_MAILBOX_KHR` (fallback FIFO) with triple buffering, a 3-slot frame-in-flight pool, and per-swapchain-image fences, so the CPU overlaps frames instead of draining the queue (details in [`FramePacingPlan.md`](FramePacingPlan.md)). The loop is unpaced by default with an optional `--cap <fps>` frame cap.

## Ownership and performance

Startup, frame, and persistent allocation domains are separate. Runtime hot paths use structs, spans, pools, explicit loops, and preallocated buffers. Reflection and LINQ are prohibited in runtime projects. Managed allocations are measured; zero GC is a target for steady state, not an unverified promise.

## Modules

Core owns handles, timing, results, and lifecycle contracts. Mathematics owns SIMD-friendly value types and isometric transforms. Threading owns workers and job dependencies. ECS owns archetypes, chunks, queries, and deferred commands. Rendering owns backend-neutral packets; Rendering.Vulkan owns Vulkan objects and GPU lifetime. Audio and Physics expose backend-neutral interfaces with optional adapters. Runtime composes the modules and the sample demonstrates the vertical slice.

## Non-goals

No editor, networking, skeletal animation, deferred renderer, or production asset marketplace is required for the first prototype.
