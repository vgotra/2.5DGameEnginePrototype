using IsometricSandbox.Game.App;

Options options = Options.Parse(args);
using ArcherGameApp app = ArcherGameApp.Create(options);
app.Run();
