# Packaging and Build Policy

- ENFORCE central package management. ADD new packages to `Directory.Packages.props` (only `Vortice.Vulkan` 3.2.3 today), referenced without a version.
- APPLY the global build properties in `Directory.Build.props`: `net10.0`, `LangVersion preview`, `Nullable enable`, `ImplicitUsings enable`, `AllowUnsafeBlocks true`, `InvariantGlobalization true`.
- USE one csproj per `Engine.*` area with a minimal project file: `<AssemblyName>` equal to the project name, project/package references only. NO duplicated SDK or property blocks.
