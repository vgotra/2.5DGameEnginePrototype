# Architecture

The dependency direction is downward: sample -> runtime -> subsystems -> core. Backend-specific projects implement narrow interfaces and never leak backend handles into gameplay code.

```mermaid
flowchart TD
  Sample[IsometricSandbox] --> Runtime[Engine.Runtime]
  Runtime --> ECS[Engine.Ecs]
  Runtime --> Jobs[Engine.Threading]
  Runtime --> Render[Engine.Rendering]
  Render --> Vulkan[Engine.Rendering.Vulkan]
  Runtime --> Physics[Engine.Physics]
  Physics --> Jolt[Engine.Physics.Jolt]
  Runtime --> Audio[Engine.Audio]
  Runtime --> Platform[Engine.Platform]
  ECS --> Core[Engine.Core]
  Jobs --> Core
  Render --> Core
```
