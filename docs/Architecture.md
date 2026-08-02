# Architecture

The dependency direction is downward: sample -> contracts -> backends -> core. The sample composes the modules directly as the executable vertical slice; a formal `Engine.Runtime` composition layer is planned. Backend-specific projects implement narrow interfaces and never leak backend handles into gameplay code.

```mermaid
flowchart TD
  Sample[IsometricSandbox] --> ECS[Engine.Ecs]
  Sample --> Render[Engine.Rendering]
  Sample --> Host[Engine.Platform.Desktop]
  Sample --> Vulkan[Engine.Rendering.Vulkan]
  Render --> Vulkan
  Vulkan --> Platform[Engine.Platform]
  Host --> Platform
  Host --> Win32[Engine.Platform.Win32]
  Win32 --> Platform
  ECS --> Core[Engine.Core]
  Render --> Core
  Platform -. planned backend .-> LinuxSdl[Engine.Platform.Sdl2]
  Audio[Engine.Audio] -. no backend yet .-> Core
  Physics[Engine.Physics] -. no backend yet .-> Core
  Threading[Engine.Threading] --> Core
```

Planned (not yet projects): `Engine.Platform.Sdl2` (Linux/macOS windowing), audio and physics adapters, and a formal `Engine.Runtime` composition layer.
