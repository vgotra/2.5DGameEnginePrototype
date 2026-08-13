using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

public sealed class NpcBehaviorSystem(TileMap map) : ISystem
{
    private Query<Position, NpcState>? _query;

    public void Update(Engine.Ecs.Sparse.World world, float deltaSeconds)
    {
        _query ??= world.Query<Position, NpcState>();
        Body body = new() { Map = map, DeltaSeconds = deltaSeconds };
        _query.ForEach(ref body);
    }

    private struct Body : IQueryAction<Position, NpcState, Body>
    {
        public TileMap Map;
        public float DeltaSeconds;

        public static void Execute(ref Body body, Entity entity, ref Position position, ref NpcState npc)
        {
            if (npc.WanderTarget == Vector2.Zero) npc.WanderTarget = position.Value;
            Vector2 to = npc.WanderTarget - position.Value;
            if (to.LengthSquared() < 0.01f) { npc.Behavior = 0; return; }
            npc.Behavior = 1;
            Vector2 next = position.Value + Vector2.Normalize(to) * npc.Speed * body.DeltaSeconds;
            if (body.Map.CanOccupy(next, npc.Radius)) position.Value = next;
        }
    }
}
