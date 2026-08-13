using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

// Snapshot of every living critter, gathered before the arrows move so a
// whole flight sees the same herd state (and a critter can die at most once
// per step). Sized to the normal-mode population; simulation critters have
// no Health component so they are skipped entirely.
public struct CritterCollectorBody : IQueryAction<Position, Critter, Health, CritterCollectorBody>
{
    public Entity[] Entities;
    public Vector2[] Positions;
    public float[] Radii;
    public int[] HealthValues;
    public int Count;

    public static void Execute(ref CritterCollectorBody body, Entity entity, ref Position position, ref Critter critter, ref Health health)
    {
        if (body.Count >= body.Entities.Length) return;
        body.Entities[body.Count] = entity;
        body.Positions[body.Count] = position.Value;
        body.Radii[body.Count] = critter.Radius;
        body.HealthValues[body.Count] = health.Value;
        body.Count++;
    }
}
