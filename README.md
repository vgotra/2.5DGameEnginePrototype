# 2.5D Isometric Game Engine

Windows-first .NET 10 prototype using SDL3, Vulkan, and a sparse-set ECS. The runtime is an isometric 2.5D engine with explicit frame scheduling and renderer-neutral gameplay contracts.

## Build and run

```powershell
dotnet build Engine.slnx
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --frames 10 --cap 0
```

The bounded sample command is suitable for automated verification. Omit `--frames` only for an interactive run.

## Benchmarks

`benchmarks\Engine.Benchmark` is retained for opt-in performance-regression checks. Benchmark results are machine-sensitive; use the project’s documented comparison and baseline commands when investigating performance changes.

Project-specific agent instructions are in [`AGENTS.md`](AGENTS.md). Durable implementation notes are limited to the context files that remain in `.agents\context`.
