# Running, Verifying, and Testing

Single reference for prerequisites, building, running, and verifying the engine and sample. This is the one source of truth for the commands below — other files link here instead of repeating them. All commands run from the repository root.

- [`Prerequisites.md`](Prerequisites.md) — Windows, .NET 10 SDK, and Vulkan SDK requirements.
- [`Build.md`](Build.md) — `dotnet build Engine.slnx --nologo` (0 errors).
- [`Test.md`](Test.md) — `dotnet run --project tests\Engine.Tests\Engine.Tests.csproj` smoke tests.
- [`Run.md`](Run.md) — `dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj`, flags, controls.
- [`Benchmarking.md`](Benchmarking.md) — perf/alloc benchmark harness and session-to-session comparison.
- [`Verify.md`](Verify.md) — the after-change verification checklist.

## Related

- GLSL shaders recompile automatically on the next build (`glslc`, incremental); see [`ShaderWorkflow.md`](../ShaderWorkflow.md).
- Coding and performance conventions: [`Conventions/`](../Conventions/).
