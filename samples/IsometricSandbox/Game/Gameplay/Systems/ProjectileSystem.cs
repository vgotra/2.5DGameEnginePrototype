using System.Numerics;
using Engine.App;
using Engine.Core;
using Engine.Ecs.Sparse;
using IsometricSandbox.Game.Configuration;
using IsometricSandbox.Game.Gameplay.Components;
using SparseWorld = Engine.Ecs.Sparse.World;

namespace IsometricSandbox.Game.Gameplay.Systems;

public sealed class ProjectileSystem(TerrainSurface map) : ISystem
{
    private readonly Entity[] _targets = new Entity[SampleConfig.MaxEnemies];
    private readonly Vector2[] _targetPositions = new Vector2[SampleConfig.MaxEnemies];
    private readonly float[] _targetRadii = new float[SampleConfig.MaxEnemies];
    private readonly bool[] _enemyTargets = new bool[SampleConfig.MaxEnemies];
    private Query<Position, ArrowProjectile>? _arrowQuery;
    private Query<Position, MonsterState, Faction>? _monsterQuery;

    public EntityCommands Buffer { get; set; } = new();
    public int LastKills { get; private set; }


    public void Update(SparseWorld world, float deltaSeconds)
    {
        LastKills = 0;
        _arrowQuery ??= world.Query<Position, ArrowProjectile>();
        if (_arrowQuery.Count == 0) return;
        _monsterQuery ??= world.Query<Position, MonsterState, Faction>();
        MonsterCollectorBody monsterCollector = new() { Entities = _targets, Positions = _targetPositions, Radii = _targetRadii, EnemyTargets = _enemyTargets };
        _monsterQuery.ForEach(ref monsterCollector);
        ProjectileBody body = new()
        {
            Map = map,
            Buffer = Buffer,
            World = world,
            Entities = _targets,
            Positions = _targetPositions,
            Radii = _targetRadii,
            CritterCount = monsterCollector.Count,
            EnemyTargets = _enemyTargets,
            DeltaSeconds = deltaSeconds,
            DamageAmount = 1,
        };
        _arrowQuery.ForEach(ref body);
        LastKills = body.Kills;
        LastEnemyKills = body.EnemyKills;
    }

    public int LastEnemyKills { get; private set; }
}
