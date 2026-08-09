using Engine.Core;
using Engine.Ecs;

namespace IsometricSandbox.Game;

public struct ArrowCollectBody : IForEach<ArrowProjectile, ArrowCollectBody>
{
    public WorldCommandBuffer Buffer;

    public static void Execute(ref ArrowCollectBody body, EntityId entity, ref ArrowProjectile arrow) => body.Buffer.Destroy(entity);
}
