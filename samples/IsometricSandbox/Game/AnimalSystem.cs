using System.Numerics;
using System.Runtime.CompilerServices;

namespace IsometricSandbox.Game;

public static class AnimalSystem
{
    public const float RespawnDelay = 6f;
    private const float FleeRadius = 3.5f;

    public static Animal Create(AnimalSpecies species, Vector2 position)
    {
        bool deer = species == AnimalSpecies.Deer;
        return new Animal
        {
            Position = position,
            Species = species,
            Speed = deer ? 1.4f : 2.2f,
            Radius = deer ? 0.5f : 0.35f,
            Alive = true,
        };
    }

    public static void Update(ref Animal animal, TileMap map, Vector2 player, float deltaSeconds, Random random)
    {
        if (!animal.Alive)
        {
            animal.RespawnTimer -= deltaSeconds;
            return;
        }

        Vector2 away = animal.Position - player;
        if (away.LengthSquared() < FleeRadius * FleeRadius)
        {
            if (away.LengthSquared() < 0.0001f) away = new Vector2(1, 0);
            away = Vector2.Normalize(away);
            animal.Position = map.TryMove(animal.Position, animal.Position + away * animal.Speed * 2.2f * deltaSeconds, animal.Radius);
            return;
        }

        if (Vector2.DistanceSquared(animal.Position, animal.WanderTarget) < 0.05f)
            animal.WanderTarget = RandomWalkableTile(map, animal.Position, random);

        Vector2 direction = animal.WanderTarget - animal.Position;
        if (direction.LengthSquared() < 0.0001f)
        {
            animal.WanderTarget = RandomWalkableTile(map, animal.Position, random);
            return;
        }
        direction = Vector2.Normalize(direction);
        animal.Position = map.TryMove(animal.Position, animal.Position + direction * animal.Speed * deltaSeconds, animal.Radius);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 RandomWalkableTile(TileMap map, Vector2 near, Random random)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float x = Math.Clamp(near.X + (random.NextSingle() * 2f - 1f) * 6f, 1f, map.Width - 2f);
            float y = Math.Clamp(near.Y + (random.NextSingle() * 2f - 1f) * 6f, 1f, map.Height - 2f);
            if (map.CanOccupy(new Vector2(x, y), 0.4f)) return new Vector2(x, y);
        }
        return near;
    }
}
