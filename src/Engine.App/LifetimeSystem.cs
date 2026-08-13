using Engine.Ecs.Sparse;

namespace Engine.App;

public sealed class LifetimeSystem : ISystem
{
    private Query<Lifetime>? _query;

    public EntityCommands Buffer { get; set; } = new();

    public void Update(Engine.Ecs.Sparse.World world, float deltaSeconds)
    {
        _query ??= world.Query<Lifetime>();
        LifetimeBody body = new() { DeltaSeconds = deltaSeconds, Buffer = Buffer };
        _query.ForEach(ref body);
    }

    private struct LifetimeBody : IQueryAction<Lifetime, LifetimeBody>
    {
        public float DeltaSeconds;
        public EntityCommands Buffer;

        public static void Execute(ref LifetimeBody body, Entity entity, ref Lifetime lifetime)
        {
            lifetime.Remaining -= body.DeltaSeconds;
            if (lifetime.Remaining <= 0f) body.Buffer.Destroy(entity);
        }
    }
}
