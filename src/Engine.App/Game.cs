namespace Engine.App;

public abstract class Game
{
    private readonly List<World> _worlds = new();

    public World? ActiveWorld { get; private set; }

    protected World CreateWorld(string name)
    {
        World world = new(name);
        _worlds.Add(world);
        ActiveWorld = world;
        return world;
    }

    protected virtual void Initialize() { }
    protected virtual void Shutdown() { }

    protected void InitializeGame() => Initialize();
    protected void ShutdownGame() => Shutdown();
}
