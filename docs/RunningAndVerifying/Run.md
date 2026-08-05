# Run the sample

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj
```

- Opens an 800x600 window centered on the primary screen; requires the Vulkan SDK.
- Renders the "Archer in the Forest" mini-game: a forest map with a river (bridges), a flickering bonfire, and wandering deer/rabbits. The archer aims at the mouse cursor and shoots arrows; animals respawn after a delay. Sprites are drawn from `assets/textures/*.png` when present, falling back to procedural/colored art otherwise (see [`AssetWorkflow.md`](../AssetWorkflow.md)).
- The camera clamps to the map bounds and centers the map on screen when it fits the viewport (fullscreen); in windowed mode it follows the player.

## Flags

| Flag | Effect |
| --- | --- |
| `--2d` | Flat top-down view instead of the isometric diamond view. |
| `--fullscreen` | Start the window in borderless fullscreen. |
| `--cap <fps>` | Cap the frame rate (e.g. `--cap 60`); default is unpaced. |
| `--metrics` | Print a rolling table every 120 frames: avg/max frame time, sim steps, sprites, avg allocated bytes/frame, GC collections (see [`Benchmarking.md`](Benchmarking.md)). |

Example:

```powershell
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --2d
```

## Controls

- `WASD` or arrow keys — move.
- `Mouse` — aim the archer.
- `Left click` — shoot an arrow at the cursor.
- `Space` — jump two tiles in the last movement direction.
- `R` — restart the game (fresh animals and score).
- `F11` — toggle borderless fullscreen (the swapchain is rebuilt on the size change).
- `Escape` — close the game.

The current score is shown in the window title.

Window drag-resizing goes through the same swapchain-rebuild path as fullscreen toggling.
