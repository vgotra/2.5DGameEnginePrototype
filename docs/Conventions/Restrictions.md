# Restrictions (Platform Neutrality)

- Contracts and shared gameplay code never contain OS-specific P/Invoke or types.
- OS-specific code lives in `Engine.Platform.*` backends; the renderer consumes `NativeWindowSurface`; backend selection happens in `Engine.Platform.Desktop.GamePlatform`. See [`LinuxSupportPlan.md`](../LinuxSupportPlan.md).
