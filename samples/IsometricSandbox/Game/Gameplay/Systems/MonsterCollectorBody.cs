using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game.Gameplay.Systems;

public struct MonsterCollectorBody : IQueryAction<Position, MonsterState, Faction, MonsterCollectorBody>
{
    public Entity[] Entities;
    public Vector2[] Positions;
    public float[] Radii;
    public bool[] EnemyTargets;
    public int Offset;
    public int Count;

    public static void Execute(ref MonsterCollectorBody body, Entity entity, ref Position position, ref MonsterState monster, ref Faction faction)
    {
        if (faction.Team != Team.Enemy) return;
        int index = body.Offset + body.Count;
        if (index >= body.Entities.Length) return;
        body.Entities[index] = entity;
        body.Positions[index] = position.Value;
        body.Radii[index] = monster.Radius;
        body.EnemyTargets[index] = true;
        body.Count++;
    }
}
