using IsometricSandbox.Game;

// ====================================================================
//  IsometricSandbox — "Archer in the Forest"
//  A small end-to-end sample of the engine. This file only parses the
//  command line and runs the ECS game app; the work lives in Game\:
//    Options       — command-line flags (--cap, --frames, --fullscreen, --metrics, --parallel)
//    ArcherGameApp — the GameHost: window/renderer/world wiring + frame loop
//    SampleConfig  — the tunables (window size, speeds, sim scale, ...)
//
//  HOW TO RUN     : dotnet run --project samples\IsometricSandbox
//  BOUNDED RUN    : dotnet run --project samples\IsometricSandbox -- --frames 60 --cap 0
//  MAKE CHANGES   : edit Game\SampleConfig.cs — the tunables live there.
// ====================================================================

Options options = Options.Parse(args);
using ArcherGameApp app = ArcherGameApp.Create(options);
app.Run();
