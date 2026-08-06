# Commands

- SEE `../RunningAndVerifying/` for build, test, run, flags, controls, and the verification checklist.
- NEVER use `dotnet test`. RUN the brief smoke tests as a plain console app — SEE `../RunningAndVerifying/` for the command.
- GLSL shaders are recompiled automatically at build time (incremental, `glslc`); `tools\CompileShaders.ps1` is the manual fallback. SEE `../ShaderWorkflow.md`.
