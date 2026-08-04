# Engine Development

Read the current context files (`.agents/context/`) and use the `mcp-repo-graph` MCP server for the codebase structural map before changing engine code. Preserve dependency direction, avoid reflection and LINQ in runtime projects, prefer explicit ownership and low-allocation APIs, and update recovery state after each milestone. Verify with tests and benchmarks whenever the environment permits.

Develop with SOLID, KISS, and DRY: small single-responsibility types and methods, the smallest solution that works, and code that is easy to understand, refactor, and support — follow existing patterns rather than adding speculative abstraction.

Keep code platform-neutral: contracts and shared gameplay code never contain OS-specific P/Invoke or types. OS-specific code lives in `Engine.Platform.*` backends; the renderer consumes `NativeWindowSurface`, and platform selection happens in `Engine.Platform.Desktop.GamePlatform`. Linux (via SDL2) and macOS are planned, not current — do not add Win32-only parameters to contracts or `IRenderer`.
