namespace Engine.App;

public sealed class SceneManager
{
    private readonly Game _game;
    private World? _world;

    internal SceneManager(Game game) => _game = game;

    public Scene? ActiveScene => _world?.ActiveScene;

    public Scene LoadScene(SceneId id)
    {
        if (id.Value is null) throw new ArgumentException("Scene ID is required.", nameof(id));
        World world = EnsureWorld();
        if (_game.Content.TryGet(id, out SceneDefinition definition)) return world.LoadScene(in definition);
        return world.LoadScene(id.Value);
    }

    public Scene LoadScene(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return LoadScene(new SceneId(id));
    }

    public void StartScene(SceneId id)
    {
        if (id.Value is null) throw new ArgumentException("Scene ID is required.", nameof(id));
        StartScene(id.Value);
    }

    public void StartScene(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        EnsureWorld().ChangeScene(id);
    }

    public void UnloadScene(SceneId id)
    {
        if (id.Value is null) throw new ArgumentException("Scene ID is required.", nameof(id));
        EnsureWorld().UnloadScene(id.Value);
    }

    internal void Attach(World world) => _world = world;

    private World EnsureWorld()
        => _world ?? _game.CreatePublicWorld();
}
