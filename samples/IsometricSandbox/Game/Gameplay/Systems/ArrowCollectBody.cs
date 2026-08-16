using Engine.Ecs.Sparse;
using IsometricSandbox.Game.Gameplay.Components;

namespace IsometricSandbox.Game.Gameplay.Systems;

public struct ArrowCollectBody : IQueryAction<ArrowProjectile, ArrowCollectBody>
{
    public EntityCommands Buffer;

    public static void Execute(ref ArrowCollectBody body, Entity entity, ref ArrowProjectile arrow) => body.Buffer.Destroy(entity);
}
