using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

public struct ArrowCollectBody : IQueryAction<ArrowProjectile, ArrowCollectBody>
{
    public WorldCommandBuffer Buffer;

    public static void Execute(ref ArrowCollectBody body, Entity entity, ref ArrowProjectile arrow) => body.Buffer.Destroy(entity);
}
