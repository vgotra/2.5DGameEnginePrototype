# Game Engine Design

## Goals

The engine is a Crossplatform (Windows, Linux, macOS later) .NET 10 runtime for 2D/2.5D isometric games. It uses Vortice.Vulkan for explicit GPU control and keeps gameplay-facing contracts independent from platform, audio, and physics backends. Window/input/Vulkan surface run on SDL3 on all OSes; Linux (X11/Wayland) and macOS verification is planned (see `docs/LinuxSupportPlan.md`).
