using System.Numerics;
using Engine.App;
using Engine.Core;
using Engine.Ecs.Sparse;
using World = Engine.Ecs.Sparse.World;

namespace IsometricSandbox.Game;

// Moves every critter. Normal mode runs serially with the same random
// wander/flee logic as the original animals; simulation mode runs a
// deterministic, allocation-free wander in parallel so a huge herd is a
// real job-system stress test.
public sealed class CritterSystem(TerrainSurface map, bool simulation) : ISystem
{
    private readonly Random _random = new(1337);
    private float _time;
    public Entity Player { get; set; }

    public void Update(World world, float deltaSeconds)
    {
        if (simulation)
        {
            SimCritterBody body = new() { Map = map, Time = _time, DeltaSeconds = deltaSeconds };
            world.Query<Position, Critter>().ForEach(ref body);
            _time += deltaSeconds;
            return;
        }
        else
        {
            // Reads the player position without declaring it; sequential
            // scheduling keeps this system after the player writes it.
            CritterWanderBody body = new()
            {
                Map = map,
                Random = _random,
                Player = world.Get<Position>(Player).Value,
                DeltaSeconds = deltaSeconds,
            };
            world.Query<Position, Critter>().ForEach(ref body);
        }
        _time += deltaSeconds;
    }

    public static Critter Create(AnimalSpecies species, Vector2 position)
    {
        bool deer = species == AnimalSpecies.Deer;
        return new Critter
        {
            Species = species,
            Speed = deer ? SampleConfig.DeerSpeed : SampleConfig.RabbitSpeed,
            Radius = deer ? SampleConfig.DeerRadius : SampleConfig.RabbitRadius,
            WanderTarget = position,
        };
    }

    // A random walkable tile a few tiles from `near`, used as a wander target.
    public static Vector2 RandomWalkableTile(TerrainSurface map, Vector2 near, Random random)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float x = Math.Clamp(near.X + (random.NextSingle() * 2f - 1f) * 6f, 1f, map.Width - 2f);
            float y = Math.Clamp(near.Y + (random.NextSingle() * 2f - 1f) * 6f, 1f, map.Height - 2f);
            if (map.CanOccupy(new Vector2(x, y), 0.4f)) return new Vector2(x, y);
        }
        return near;
    }

    // A random walkable spawn at least 4 tiles from the player start.
    public static Vector2 FindSpawn(TerrainSurface map, Random random, Vector2 playerStart)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = random.Next(1, map.Width - 1);
            int y = random.Next(1, map.Height - 1);
            Vector2 pos = map.TileToWorld(x, y);
            if (map.IsWalkable(x, y) && Vector2.DistanceSquared(pos, playerStart) > 16f) return pos;
        }
        return map.TileToWorld(10, 10);
    }
}
