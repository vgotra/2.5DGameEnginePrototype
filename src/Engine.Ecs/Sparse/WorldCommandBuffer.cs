namespace Engine.Ecs.Sparse;

public sealed class WorldCommandBuffer
{
    private readonly List<Entity> _destroys = new();
    private readonly List<Command> _commands = new();

    public void Destroy(Entity entity) => _destroys.Add(entity);

    public void RemoveComponent<T>(Entity entity) where T : unmanaged
        => _commands.Add(new RemoveCommand<T>(entity));

    public void Clear()
    {
        _destroys.Clear();
        _commands.Clear();
    }

    public void Apply(World world)
    {
        for (int i = 0; i < _commands.Count; i++) _commands[i].Apply(world);
        for (int i = 0; i < _destroys.Count; i++) world.Destroy(_destroys[i]);
    }

    private abstract class Command
    {
        public abstract void Apply(World world);
    }

    private sealed class RemoveCommand<T>(Entity entity) : Command where T : unmanaged
    {
        public override void Apply(World world) => world.Remove<T>(entity);
    }
}
