using Engine.Core;
using Engine.Ecs;

namespace IsometricSandbox.Game;

public struct CritterCollectBody : IForEach<Critter, CritterCollectBody>
{
    public WorldCommandBuffer Buffer;

    public static void Execute(ref CritterCollectBody body, EntityId entity, ref Critter critter) => body.Buffer.Destroy(entity);
}
