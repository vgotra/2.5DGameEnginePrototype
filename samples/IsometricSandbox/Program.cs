using IsometricSandbox.Game;

// ====================================================================
//  IsometricSandbox — "Archer in the Forest"
//  A small end-to-end sample of the engine. This file only parses the
//  command line and runs a GameSession; the work lives in Game\:
//    Options        — command-line flags (--2d, --cap, --fullscreen, --metrics)
//    GameSession    — window/renderer/world wiring + the frame loop
//    Player         — the archer's movement, jump, and aiming
//    SceneRenderer  — the Vulkan render path + sprite extraction buffers
//    SampleConfig   — the tunables (window size, animal count, ...)
//
//  HOW TO RUN     : dotnet run --project samples\IsometricSandbox
//  MAKE CHANGES   : edit Game\SampleConfig.cs — the tunables live there.
// ====================================================================

Options options = Options.Parse(args);
using GameSession session = new(options);
session.Run();
