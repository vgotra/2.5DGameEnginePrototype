using IsometricSandbox.Game;

// ====================================================================
//  IsometricSandbox — "Archer in the Forest"
//  A small end-to-end sample of the engine. This file only parses the
//  command line and runs the ECS game app; the work lives in Game\:
//    Options       — command-line flags (--2d, --cap, --frames, --fullscreen, --metrics, --parallel, --simulation, --arpg)
//    ArcherGameApp — the GameHost: window/renderer/world wiring + frame loop
//    SampleConfig  — the tunables (window size, speeds, sim scale, ...)
//
//  HOW TO RUN     : dotnet run --project samples\IsometricSandbox
//  STRESS TEST    : dotnet run --project samples\IsometricSandbox -- --simulation
//  BOUNDED RUN    : dotnet run --project samples\IsometricSandbox -- --arpg --frames 10
//  MAKE CHANGES   : edit Game\SampleConfig.cs — the tunables live there.
// ====================================================================

Options options = Options.Parse(args);
using ArcherGameApp app = ArcherGameApp.Create(options);
app.Run();
