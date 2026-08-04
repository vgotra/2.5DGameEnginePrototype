# Game Engine Design

## Goals

The engine is a Crossplatform (Windows, Linux, macOS later) .NET 10 runtime for 2D/2.5D isometric games. It uses Vortice.Vulkan for explicit GPU control and keeps gameplay-facing contracts independent from platform, audio, and physics backends. Linux (X11/Wayland via SDL2) and macOS are planned targets; platform seams (`IGameWindow`, `IInputState`, `NativeWindowSurface`, `GamePlatform`) are in place today (see `docs/LinuxSupportPlan.md`).
