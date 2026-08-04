# RenderDoc Setup (local + agent usage)

How to install RenderDoc locally, capture frames from the sample, and analyze captures through opencode's `renderdoc` MCP server. The `renderdoc` MCP entry is configured in `opencode.json` / `.agents/mcp.json` (see `docs/AgentTooling.md`).

## Two roles, two tools

RenderDoc is used here in two separate ways:

| Role | What runs it | Needs a RenderDoc install? |
| --- | --- | --- |
| **Capture** `.rdc` frames from the running Vulkan app | RenderDoc GUI (or its Vulkan capture layer) | Yes — you install it locally. |
| **Analyze** `.rdc` files | `renderdoc` MCP server (`uvx --python 3.13 --with "mcp>=1.0,<2" renderdoc-mcp`), invoked by opencode | No — the MCP package bundles its own RenderDoc replay module (Python 3.13), so analysis works even without a local RenderDoc install. |

Capturing and analyzing are independent: you can capture on one machine (or with the GUI) and analyze anywhere the MCP server runs.

## 1. Install RenderDoc

1. Download the latest Windows build from <https://renderdoc.org/builds>.
2. Run the installer and keep the defaults. The "RenderDoc" desktop app (`qrenderdoc.exe`) and the capture layer are installed together.
3. Verify: `Test-Path "C:\Program Files\RenderDoc\renderdoc.pyd"` — if present, the install is complete (that Python module is only needed if you use the Python-based capture API; the MCP server bundles its own).

## 2. Capture a frame from the sample

The sample renders on the GPU through the Vulkan `IRenderer` path (the only render path).

1. Build the sample first (0 errors required — see `docs/RunningAndVerifying/`).
2. Open RenderDoc → **Launch Application**.
   - **Executable:** `samples\IsometricSandbox\bin\Debug\net10.0\IsometricSandbox.exe` (or `bin\Release\net10.0\`). Launch the apphost exe directly, not via `dotnet run`, so RenderDoc sees the Vulkan calls in-process.
   - **Arguments:** optional `--2d`, `--fullscreen` also capture fine.
   - **Working directory:** any directory — shaders load from `AppContext.BaseDirectory\shaders`, not the working directory.
   - **Environment / layer injection:** leave defaults; RenderDoc injects `VK_LAYER_RENDERDOC_Capture` automatically for the launched process.
3. Click **Launch**, then press **Ctrl+F11** in the sample window to capture a frame. Capture a few frames to a file (**File → Save capture**) as e.g. `frame.rdc`.

## 3. Analyze a capture through opencode

1. Restart opencode so the MCP config from `opencode.json` loads (`.agents/README.md`).
2. In a session, point the agent at the capture:
   ```
   Open C:\Projects\2.5DGameEnginePrototype\frame.rdc and summarize this frame's draw calls.
   ```
   The agent uses the `renderdoc` MCP tools (frame navigation, draw-call/pipeline state, texture/buffer inspection, pixel history). The server is started on demand by `uvx --python 3.13 --with "mcp>=1.0,<2" renderdoc-mcp`; the first call downloads it and its bundled RenderDoc module.
3. Verify the server is wired up by asking for the available MCP tools; you should see the `renderdoc_*` tools.

## Troubleshooting

- **`uvx` not found** — install [uv](https://docs.astral.sh/uv/getting-started/installation/). The `--python 3.13` flag is required: the bundled RenderDoc module is compiled only for Python 3.13.
- **`renderdoc` MCP fails to start (`server unavailable`)** — `renderdoc-mcp` requires `mcp>=1.0.0` but breaks on `mcp` 2.x (`mcp.server.fastmcp` was removed), so the launch pins `--with "mcp>=1.0,<2"`. Keep that pin in both `opencode.json` and `.agents/mcp.json`; if it regresses, run `uvx --python 3.13 --with "mcp>=1.0,<2" renderdoc-mcp` and send an MCP `initialize` line to see the error.
- **Frame capture produces nothing / black frame** — the sample must run on a Vulkan-capable driver. Rebuild after editing `assets/shaders/*.glsl` (see `docs/ShaderWorkflow.md`).
- **Capture layer not injected** — prefer **Launch Application** over attaching; RenderDoc can only capture a process it launched (or one that enabled the layer itself).
- **Analysis errors on an `.rdc` from another machine** — the bundled replay module replays captures locally on the GPU; make sure a compatible Vulkan driver is present. CPU-based replay is not used.
- **Analysis needs a RenderDoc install?** No — only *capture* does. If you never capture locally, skip step 1 entirely.
