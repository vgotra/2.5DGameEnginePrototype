# Conventions

Coding, performance, packaging, and build conventions that differ from plain .NET defaults. Principles are in `../AGENTS.md` (`## Principles`).

## Performance and allocation rules

- **No reflection, no LINQ, no managed allocation in runtime hot paths.** `src/` currently has zero `using System.Linq`. Use structs, spans, explicit loops, and preallocated buffers.
- Startup, frame, and persistent allocation domains are separate. Zero-GC is a **target for steady state**, not an unverified promise.
- Do **not** schedule jobs for a 20x20 tile loop — job overhead exceeds the work. Parallelize only asset decoding, shader compilation, uploads, and extraction for maps above roughly 10,000 visible tiles.
- Keep fixed-step player movement/collision on the main thread for deterministic ordering.

## Packages and build policy

- Central package management: new packages go into `Directory.Packages.props` (only `Vortice.Vulkan` 3.2.3 today), referenced without a version.
- `TreatWarningsAsErrors` is on — builds must be 0 warnings.
- `global.json` pins the .NET 10 SDK (10.0.100, prerelease allowed); projects are `net10.0` with `LangVersion preview`.

## Commands

```powershell
dotnet build Engine.sln --nologo                    # must be 0 warnings
dotnet run --project tests\Engine.Tests\Engine.Tests.csproj   # prints "Smoke tests passed"; NOT dotnet test
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --vulkan   # opens a window
# flags: --vulkan (default), --gdi, --2d, --fullscreen; F11 fullscreen, Escape exits
tools\CompileShaders.ps1                            # recompile GLSL after edits
```

## Platform neutrality

- Contracts and shared gameplay code never contain OS-specific P/Invoke or types.
- OS-specific code lives in `Engine.Platform.*` backends; the renderer consumes `NativeWindowSurface`; backend selection happens in `Engine.Platform.Desktop.GamePlatform`. See [`LinuxSupportPlan.md`](LinuxSupportPlan.md).
