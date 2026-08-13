using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;

namespace IsometricSandbox.Game;

public struct ProjectileBody : IQueryAction<Position, ArrowProjectile, ProjectileBody>
{
    private const float HomingRadiusSquared = SampleConfig.HomingRadius * SampleConfig.HomingRadius;

    public TerrainSurface Map;
    public EntityCommands Buffer;
    public Entity[] Entities;
    public Vector2[] Positions;
    public float[] Radii;
    public int[] HealthValues;
    public int CritterCount;
    public float DeltaSeconds;
    public int Kills;
    public int DamageAmount;

    public static void Execute(ref ProjectileBody body, Entity entity, ref Position position, ref ArrowProjectile arrow)
    {
        arrow.Lifetime -= body.DeltaSeconds;
        Vector2 current = position.Value;
        Steer(ref body, ref arrow, current);
        Vector2 next = current + arrow.Direction * arrow.Speed * body.DeltaSeconds;
        bool blocked = !body.Map.CanOccupy(next, ArrowProjectile.Radius);
        if (arrow.Lifetime <= 0f || blocked)
        {
            body.Buffer.Destroy(entity);
            return;
        }
        for (int i = 0; i < body.CritterCount; i++)
        {
            if (!body.Entities[i].IsValid) continue;
            float combined = ArrowProjectile.Radius + body.Radii[i];
            if (Vector2.DistanceSquared(body.Positions[i], next) < combined * combined)
            {
                body.Buffer.Destroy(entity);
                body.HealthValues[i] -= body.DamageAmount;
                if (body.HealthValues[i] <= 0)
                {
                    body.Buffer.Destroy(body.Entities[i]);
                    body.Entities[i] = default;
                    body.Kills++;
                }
                return;
            }
        }
        position.Value = next;
    }

    private static void Steer(ref ProjectileBody body, ref ArrowProjectile arrow, Vector2 position)
    {
        int nearest = -1;
        float best = float.MaxValue;
        for (int i = 0; i < body.CritterCount; i++)
        {
            if (!body.Entities[i].IsValid) continue;
            float distanceSquared = Vector2.DistanceSquared(body.Positions[i], position);
            if (distanceSquared < best)
            {
                best = distanceSquared;
                nearest = i;
            }
        }
        if (nearest < 0 || best > HomingRadiusSquared) return;
        Vector2 to = body.Positions[nearest] - position;
        if (to.LengthSquared() < 0.0001f) return;
        arrow.Direction = Vector2.Normalize(to);
    }
}
