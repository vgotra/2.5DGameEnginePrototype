namespace Engine.Ecs.Sparse;

public sealed class EntityCommands
{
    private readonly List<Command> _commands = new();

    public Entity Create(World world)
    {
        Entity entity = world.Entities.Reserve();
        _commands.Add(new CreateCommand(entity));
        return entity;
    }

    public void Destroy(Entity entity) => _commands.Add(new DestroyCommand(entity));

    public void Add<T>(Entity entity, in T component) where T : unmanaged
        => _commands.Add(new AddCommand<T>(entity, component));

    public void Remove<T>(Entity entity) where T : unmanaged
        => _commands.Add(new RemoveCommand<T>(entity));

    public void Clear()
    {
        _commands.Clear();
    }

    public void Apply(World world)
    {
        for (int i = 0; i < _commands.Count; i++) _commands[i].Apply(world);
    }

    private abstract class Command
    {
        public abstract void Apply(World world);
    }

    private sealed class CreateCommand(Entity entity) : Command
    {
        public override void Apply(World world) => world.Entities.Activate(entity);
    }

    private sealed class DestroyCommand(Entity entity) : Command
    {
        public override void Apply(World world) => world.Destroy(entity);
    }

    private sealed class AddCommand<T>(Entity entity, T component) : Command where T : unmanaged
    {
        public override void Apply(World world) => world.Add(entity, component);
    }

    private sealed class RemoveCommand<T>(Entity entity) : Command where T : unmanaged
    {
        public override void Apply(World world) => world.Remove<T>(entity);
    }
}
