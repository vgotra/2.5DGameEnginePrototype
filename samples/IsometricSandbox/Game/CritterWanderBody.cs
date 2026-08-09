using System.Numerics;
using Engine.App;
using Engine.Core;
using Engine.Ecs;

namespace IsometricSandbox.Game;

public struct CritterWanderBody : IForEach<Position, Critter, CritterWanderBody>
{
    private const float FleeRadiusSquared = SampleConfig.FleeRadius * SampleConfig.FleeRadius;

    public TileMap Map;
    public Random Random;
    public Vector2 Player;
    public float DeltaSeconds;

    public static void Execute(ref CritterWanderBody body, EntityId entity, ref Position position, ref Critter critter)
    {
        Vector2 away = position.Value - body.Player;
        if (away.LengthSquared() < FleeRadiusSquared)
        {
            if (away.LengthSquared() < 0.0001f) away = new Vector2(1, 0);
            away = Vector2.Normalize(away);
            position.Value = body.Map.TryMove(position.Value, position.Value + away * critter.Speed * 2.2f * body.DeltaSeconds, critter.Radius);
            return;
        }
        if (Vector2.DistanceSquared(position.Value, critter.WanderTarget) < 0.05f)
            critter.WanderTarget = CritterSystem.RandomWalkableTile(body.Map, position.Value, body.Random);
        Vector2 direction = critter.WanderTarget - position.Value;
        if (direction.LengthSquared() < 0.0001f)
        {
            critter.WanderTarget = CritterSystem.RandomWalkableTile(body.Map, position.Value, body.Random);
            return;
        }
        direction = Vector2.Normalize(direction);
        position.Value = body.Map.TryMove(position.Value, position.Value + direction * critter.Speed * body.DeltaSeconds, critter.Radius);
    }
}
