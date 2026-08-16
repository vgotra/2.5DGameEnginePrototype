using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using IsometricSandbox.Game.Configuration;
using SparseWorld = Engine.Ecs.Sparse.World;

namespace IsometricSandbox.Game.Gameplay.Systems;

public sealed class MonsterMovementSystem(TerrainSurface map) : ISystem
{
    public void Update(SparseWorld world, float deltaSeconds)
    {
        MonsterMovementBody body = new() { Map = map, DeltaSeconds = deltaSeconds };
        world.Query<Position, MonsterState, Faction>().ForEach(ref body);
    }
}

public struct MonsterMovementBody : IQueryAction<Position, MonsterState, Faction, MonsterMovementBody>
{
    public TerrainSurface Map;
    public float DeltaSeconds;

    public static void Execute(ref MonsterMovementBody body, Entity entity, ref Position position, ref MonsterState monster, ref Faction faction)
    {
        if (faction.Team != Team.Enemy) return;
        Vector2 target = monster.WanderTarget;
        if (!body.Map.CanOccupy(target, monster.Radius) || Vector2.DistanceSquared(position.Value, target) < 0.09f)
            target = NextTarget(body.Map, position.Value, entity);
        monster.WanderTarget = target;
        Vector2 delta = target - position.Value;
        if (delta.LengthSquared() < 0.0001f) return;
        Vector2 direction = Vector2.Normalize(delta);
        Vector2 next = position.Value + direction * monster.Speed * body.DeltaSeconds;
        position.Value = body.Map.ResolveMove(position.Value, next, monster.Radius);
    }

    private static Vector2 NextTarget(TerrainSurface map, Vector2 position, Entity entity)
    {
        int seed = entity.Id * 1103515245 + entity.Generation * 12345;
        float x = ((seed >> 8) & 15) - 7.5f;
        float y = ((seed >> 12) & 15) - 7.5f;
        Vector2 target = new(Math.Clamp(position.X + x, 1f, map.Width - 2f), Math.Clamp(position.Y + y, 1f, map.Height - 2f));
        return map.CanOccupy(target, 0.35f) ? target : position;
    }
}
