namespace Engine.App;

public class Game
{
    private readonly List<World> _worlds = new();
    private readonly GameplayCatalog _catalog = new();

    public Game()
    {
        Scenes = new SceneManager(this);
        Content = new ContentRegistry(_catalog);
    }

    public SceneManager Scenes { get; }
    public ContentRegistry Content { get; }
    public Scene? ActiveScene => Scenes.ActiveScene;
    public WorldMap? WorldMap => ActiveWorld?.Map;
    public World? ActiveWorld { get; private set; }

    public void StartScene(SceneId id) => Scenes.StartScene(id);

    public void StartScene(string id) => Scenes.StartScene(id);

    protected World CreateWorld(string name)
    {
        World world = new(name, _catalog);
        Content.ApplyWorldMap(world.Map);
        _worlds.Add(world);
        ActiveWorld = world;
        Scenes.Attach(world);
        return world;
    }

    internal World CreatePublicWorld() => CreateWorld("Game");

    protected virtual void Initialize() { }
    protected virtual void Shutdown() { }

    protected void InitializeGame() => Initialize();
    protected void ShutdownGame() => Shutdown();
}
