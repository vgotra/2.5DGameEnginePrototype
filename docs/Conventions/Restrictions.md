# Restrictions (Platform Neutrality)

- FORBID OS-specific P/Invoke and types in contracts and shared gameplay code.
- KEEP OS-specific code in `Engine.Platform.*` backends. CONSUME `NativeWindowSurface` from the renderer. PERFORM backend selection in `Engine.Platform.Desktop.GamePlatform`. SEE `../LinuxSupportPlan.md`.
