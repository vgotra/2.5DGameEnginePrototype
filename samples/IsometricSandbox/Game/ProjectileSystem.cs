using System.Numerics;
using Engine.App;
using Engine.Core;
using Engine.Ecs.Sparse;
using World = Engine.Ecs.Sparse.World;

namespace IsometricSandbox.Game;

// Moves arrows and checks hits. Kills and expired/blocked arrows are
// buffered and applied after the whole system runs. Arrows home toward the
// nearest live critter inside the homing radius, then expire or vanish on
// map contact exactly like the original.
public sealed class ProjectileSystem(TileMap map) : ISystem
{
    private readonly Entity[] _critters = new Entity[SampleConfig.MaxAnimals];
    private readonly Vector2[] _critterPositions = new Vector2[SampleConfig.MaxAnimals];
    private readonly float[] _critterRadii = new float[SampleConfig.MaxAnimals];
    private Query<Position, ArrowProjectile>? _arrowQuery;
    private Query<Position, Critter, Health>? _critterQuery;

    public EntityCommands Buffer { get; set; } = new();
    public int LastKills { get; private set; }


    public void Update(World world, float deltaSeconds)
    {
        LastKills = 0;
        _arrowQuery ??= world.Query<Position, ArrowProjectile>();
        if (_arrowQuery.Count == 0) return;
        _critterQuery ??= world.Query<Position, Critter, Health>();
        CritterCollectorBody collector = new() { Entities = _critters, Positions = _critterPositions, Radii = _critterRadii };
        _critterQuery.ForEach(ref collector);
        ProjectileBody body = new()
        {
            Map = map,
            Buffer = Buffer,
            Entities = _critters,
            Positions = _critterPositions,
            Radii = _critterRadii,
            CritterCount = collector.Count,
            DeltaSeconds = deltaSeconds,
        };
        _arrowQuery.ForEach(ref body);
        LastKills = body.Kills;
    }
}
