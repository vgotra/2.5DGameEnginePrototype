using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

// Deterministic simulation-mode wander: each critter drifts on a smooth
// per-entity phase so the whole herd flows without any shared mutable state
// (safe to run in parallel). Positions are clamped to the walkable border.
public struct SimCritterBody : IQueryAction<Position, Critter, SimCritterBody>
{
    public TerrainSurface Map;
    public float Time;
    public float DeltaSeconds;

    public static void Execute(ref SimCritterBody body, Entity entity, ref Position position, ref Critter critter)
    {
        float phase = body.Time * 1.3f + entity.Id * 0.618f;
        float angle = MathF.Sin(phase) * MathF.PI * 2f;
        Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
        position.Value += direction * critter.Speed * body.DeltaSeconds;
        position.Value = new Vector2(
            Math.Clamp(position.Value.X, 1f, body.Map.Width - 2f),
            Math.Clamp(position.Value.Y, 1f, body.Map.Height - 2f));
    }
}
