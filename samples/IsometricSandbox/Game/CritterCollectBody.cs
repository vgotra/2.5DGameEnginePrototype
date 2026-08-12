using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

public struct CritterCollectBody : IQueryAction<Critter, CritterCollectBody>
{
    public WorldCommandBuffer Buffer;

    public static void Execute(ref CritterCollectBody body, Entity entity, ref Critter critter) => body.Buffer.Destroy(entity);
}
