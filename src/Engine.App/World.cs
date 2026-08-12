using Engine.Ecs.Sparse;

namespace Engine.App;

public sealed class World
{
    private readonly Dictionary<string, Scene> _scenes = new(StringComparer.Ordinal);
    private readonly Engine.Ecs.Sparse.World _ecsWorld = new();

    internal World(string name) => Name = name;

    public string Name { get; }
    public Scene? ActiveScene { get; private set; }
    public Engine.Ecs.Sparse.World EcsWorld => _ecsWorld;

    public Scene LoadScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (_scenes.TryGetValue(name, out Scene? existing))
        {
            ActiveScene = existing;
            return existing;
        }

        Scene scene = new(name, _ecsWorld);
        _scenes.Add(name, scene);
        ActiveScene = scene;
        return scene;
    }

    public void ChangeScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_scenes.TryGetValue(name, out Scene? scene)) throw new KeyNotFoundException($"Scene '{name}' is not loaded.");
        ActiveScene = scene;
    }

    public void UnloadScene(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_scenes.Remove(name, out Scene? scene)) return;
        scene.Unload();
        if (ReferenceEquals(ActiveScene, scene)) ActiveScene = null;
    }

    public Entity CreateEntity(EntityLifetime lifetime = EntityLifetime.Transient)
    {
        Entity entity = _ecsWorld.Create();
        if (lifetime == EntityLifetime.Scene)
        {
            if (ActiveScene is null) throw new InvalidOperationException("A scene is required for scene-owned entities.");
            ActiveScene.Register(entity, lifetime);
        }
        return entity;
    }
}
