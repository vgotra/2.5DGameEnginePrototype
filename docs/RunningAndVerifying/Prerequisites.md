# Prerequisites

- **Windows** (current supported platform; Linux/macOS are planned — see [`LinuxSupportPlan.md`](../LinuxSupportPlan.md)).
- **.NET 10 SDK** — `global.json` pins 10.0.100 (prerelease allowed); projects are `net10.0` with `LangVersion preview`.
- **Vulkan SDK** — install the latest build with the default runtime components so `vulkan-1.dll` is available. `glslc` from the SDK's `Bin` is needed only to recompile shaders (see [`ShaderWorkflow.md`](../ShaderWorkflow.md)).
