# Run the sample

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj
```

- Opens an 800x600 window centered on the primary screen; requires the Vulkan SDK.
- Renders the isometric tile map and a jumping player as white diamonds with black borders (white boxes in `--2d`) through the batched Vulkan `IRenderer` path.
- The camera clamps to the map bounds and centers the map on screen when it fits the viewport (fullscreen); in windowed mode it follows the player.

## Flags

| Flag | Effect |
| --- | --- |
| `--2d` | Flat top-down grid of white squares with black borders instead of the isometric diamond view. |
| `--fullscreen` | Start the window in borderless fullscreen. |
| `--cap <fps>` | Cap the frame rate (e.g. `--cap 60`); default is unpaced. |

Example:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --2d
```

## Controls

- `WASD` or arrow keys — move.
- `Space` — jump two tiles in the last movement direction.
- `F11` — toggle borderless fullscreen (the swapchain is rebuilt on the size change).
- `Escape` — close the game.

Window drag-resizing goes through the same swapchain-rebuild path as fullscreen toggling.
