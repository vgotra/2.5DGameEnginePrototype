using System.Numerics;
using Engine.App;
using Engine.Rendering;
using Engine.Threading;

namespace IsometricSandbox.Game;

public enum ArpgExecutionMode { Serial, AdaptiveParallel, ForcedParallel }

public readonly record struct ArpgWorkloadSnapshot(int Players, int Monsters, int Projectiles, int Effects, int AnimationFrame, int Hits, int Extracted, int Checksum);

public sealed class ArpgWorkload
{
    public const int PlayerCount = 1;
    public const int MonsterCount = 250;
    public const int ProjectileCount = 100;
    public const int EffectCount = 500;
    public const int AdaptiveThreshold = 128;

    private readonly Vector2 _playerPosition;
    private readonly Vector2[] _monsterPositions = new Vector2[MonsterCount];
    private readonly Vector2[] _monsterVelocity = new Vector2[MonsterCount];
    private readonly int[] _monsterHealth = new int[MonsterCount];
    private readonly Vector2[] _projectilePositions = new Vector2[ProjectileCount];
    private readonly Vector2[] _projectileVelocity = new Vector2[ProjectileCount];
    private readonly float[] _effectLife = new float[EffectCount];
    private readonly Vector2[] _effectPositions = new Vector2[EffectCount];
    private readonly JobSystem? _jobs;
    private readonly Action<int, int> _monsterBody;
    private readonly Action<int, int> _effectBody;
    private int _frame;
    private int _hits;
    private int _extracted;
    private int _checksum;

    public bool LastParallelDecision { get; private set; }
    public int ActiveEffectCount { get; private set; }

    public ArpgWorkload(int seed = 1337, JobSystem? jobs = null)
    {
        _jobs = jobs;
        _monsterBody = UpdateMonsters;
        _effectBody = UpdateEffects;
        Random random = new(seed);
        _playerPosition = new(10, 10);
        for (int i = 0; i < MonsterCount; i++)
        {
            _monsterPositions[i] = new(2 + random.NextSingle() * 16f, 2 + random.NextSingle() * 16f);
            _monsterVelocity[i] = new(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f);
            _monsterHealth[i] = 100;
        }
        for (int i = 0; i < ProjectileCount; i++)
        {
            _projectilePositions[i] = new(2 + random.NextSingle() * 16f, 2 + random.NextSingle() * 16f);
            _projectileVelocity[i] = new(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f);
        }
        for (int i = 0; i < EffectCount; i++)
        {
            _effectPositions[i] = new(2 + random.NextSingle() * 16f, 2 + random.NextSingle() * 16f);
            _effectLife[i] = 0.25f + random.NextSingle();
        }
        ActiveEffectCount = EffectCount;
    }

    public ArpgWorkloadSnapshot Tick(ArpgExecutionMode mode)
    {
        LastParallelDecision = mode == ArpgExecutionMode.ForcedParallel || mode == ArpgExecutionMode.AdaptiveParallel && MonsterCount >= AdaptiveThreshold;
        if (LastParallelDecision && _jobs is not null)
        {
            JobHandle monsters = _jobs.ParallelFor(MonsterCount, 32, _monsterBody);
            JobHandle effects = _jobs.ParallelFor(EffectCount, 64, _effectBody);
            _jobs.Wait(monsters);
            _jobs.Wait(effects);
        }
        else
        {
            UpdateMonsters(0, MonsterCount);
            UpdateEffects(0, EffectCount);
        }
        UpdateProjectiles();
        _frame++;
        _checksum = CalculateChecksum();
        return new ArpgWorkloadSnapshot(PlayerCount, MonsterCount, ProjectileCount, EffectCount, _frame & 7, _hits, _extracted, _checksum);
    }

    public int Extract(IsometricCamera camera, TerrainSurface grid, Span<SpritePacket> destination)
    {
        int count = 0;
        Vector2 player = camera.WorldToScreen(_playerPosition, grid);
        destination[count++] = new SpritePacket(player, new(44, 56), new(0.3f, 0.8f, 1, 1), default, default, _playerPosition.Y);
        for (int i = 0; i < MonsterCount; i++) count = Write(destination, count, camera.WorldToScreen(_monsterPositions[i], grid), new(32, 40), new(0.8f, 0.35f, 0.2f, 1), _monsterPositions[i].Y);
        for (int i = 0; i < ProjectileCount; i++) count = Write(destination, count, camera.WorldToScreen(_projectilePositions[i], grid), new(10, 10), new(1, 0.9f, 0.2f, 1), _projectilePositions[i].Y);
        for (int i = 0; i < EffectCount; i++) if (_effectLife[i] > 0) count = Write(destination, count, camera.WorldToScreen(_effectPositions[i], grid), new(18, 18), new(1, 0.4f, 0.1f, _effectLife[i]), _effectPositions[i].Y);
        _extracted = count;
        return count;
    }

    private static int Write(Span<SpritePacket> destination, int count, Vector2 position, Vector2 size, Vector4 color, float sortKey)
    {
        destination[count] = new SpritePacket(position, size, color, default, default, sortKey);
        return count + 1;
    }

    private void UpdateMonsters(int lo, int hi)
    {
        for (int i = lo; i < hi; i++)
        {
            Vector2 position = _monsterPositions[i] + _monsterVelocity[i] * (1f / 60f);
            if (position.X < 1 || position.X > 19) _monsterVelocity[i].X = -_monsterVelocity[i].X;
            if (position.Y < 1 || position.Y > 19) _monsterVelocity[i].Y = -_monsterVelocity[i].Y;
            _monsterPositions[i] = Vector2.Clamp(position, new(1), new(19));
            _monsterHealth[i] = 100 - ((_frame + i) & 15);
        }
    }

    private void UpdateProjectiles()
    {
        for (int i = 0; i < ProjectileCount; i++)
        {
            _projectilePositions[i] += _projectileVelocity[i] * (1f / 60f);
            if (_projectilePositions[i].X < 1 || _projectilePositions[i].X > 19) _projectileVelocity[i].X = -_projectileVelocity[i].X;
            if (_projectilePositions[i].Y < 1 || _projectilePositions[i].Y > 19) _projectileVelocity[i].Y = -_projectileVelocity[i].Y;
            if (Vector2.DistanceSquared(_projectilePositions[i], _monsterPositions[i % MonsterCount]) < 0.25f) _hits++;
        }
    }

    private void UpdateEffects(int lo, int hi)
    {
        for (int i = lo; i < hi; i++)
        {
            _effectLife[i] -= 1f / 60f;
            if (_effectLife[i] <= 0) _effectLife[i] = 1f;
        }
        ActiveEffectCount = EffectCount;
    }

    private int CalculateChecksum()
    {
        int value = _frame;
        for (int i = 0; i < MonsterCount; i++) value = value * 31 + (int)(_monsterPositions[i].X * 1000) + _monsterHealth[i];
        return value;
    }
}
