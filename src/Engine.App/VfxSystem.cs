using Engine.Ecs.Sparse;

namespace Engine.App;

public sealed class VfxSystem : ISystem
{
    private Query<VfxState>? _query;

    public void Update(Engine.Ecs.Sparse.World world, float deltaSeconds)
    {
        _query ??= world.Query<VfxState>();
        VfxBody body = new() { DeltaSeconds = deltaSeconds };
        _query.ForEach(ref body);
    }

    private struct VfxBody : IQueryAction<VfxState, VfxBody>
    {
        public float DeltaSeconds;

        public static void Execute(ref VfxBody body, Entity entity, ref VfxState state)
        {
            state.Time += body.DeltaSeconds;
            float duration = MathF.Max(state.Duration, 0.0001f);
            state.Opacity = 1f - Math.Clamp(state.Time / duration, 0f, 1f);
        }
    }
}
