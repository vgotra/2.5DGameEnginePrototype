# Current State

## Status

Bootstrap and gameplay slice created: SDK/build policy, core entity and clock types, isometric math utility, initial worker scheduler, ECS storage, backend-neutral contracts, deterministic tile map, continuous player movement, collision resolution, camera following, Win32 window/input with WASD and arrow keys, Vortice Vulkan package integration, Win32 surface creation, physical-device selection, logical device, graphics queue, swapchain, and acquire/present frame path.

## Verification

`dotnet build Engine.sln --nologo` passes with 0 warnings and 0 errors. `IsometricSandbox --vulkan` successfully creates the Vulkan instance, Win32 surface, physical device, logical device, graphics queue, and swapchain.

## Next three actions

1. Implement the real Vulkan shape/sprite draw path and keep GDI as a fallback.
2. Add profiling counters and allocation measurements.
3. Add explicit ECS queries/systems and background asset-loading jobs.

## Risks

The visible MVP uses the optimized double-buffered Win32 renderer. Vulkan instance, surface, physical device, logical device, queue, swapchain, synchronization, command buffers, and clear-frame submission are validated; the final Vulkan geometry draw path is still pending.
