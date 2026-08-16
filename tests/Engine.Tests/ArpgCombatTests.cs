using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;
using IsometricSandbox.Game.Configuration;
using IsometricSandbox.Game.Gameplay.Components;
using IsometricSandbox.Game.Gameplay.Systems;
using EcsWorld = Engine.Ecs.Sparse.World;

namespace Engine.Tests;

internal static class ArpgCombatTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Projectile_ExpiresWithoutTargets), Projectile_ExpiresWithoutTargets),
        new(nameof(Projectile_DamagesEnemyButNotPlayerFactionTarget), Projectile_DamagesEnemyButNotPlayerFactionTarget),
        new(nameof(Monsters_MoveDeterministicallyOnWalkableTerrain), Monsters_MoveDeterministicallyOnWalkableTerrain),
        new(nameof(EnemyExtraction_UsesNeutralTextureAndGreenVariants), EnemyExtraction_UsesNeutralTextureAndGreenVariants),
    ];

    private static TerrainSurface OpenTerrain()
    {
        TerrainSurface terrain = new(20, 20);
        for (int y = 0; y < terrain.Height; y++)
            for (int x = 0; x < terrain.Width; x++) terrain.SetTile(x, y, TileType.Floor);
        return terrain;
    }

    private static Entity CreateArrow(EcsWorld world, EntityCommands commands, Vector2 position, Vector2 direction, float lifetime)
    {
        Entity arrow = commands.Create(world);
        commands.Add(arrow, new Position(position));
        commands.Add(arrow, new ArrowProjectile { Direction = direction, Speed = SampleConfig.ArrowSpeed, Lifetime = lifetime });
        commands.Apply(world);
        commands.Clear();
        return arrow;
    }

    private static void Projectile_ExpiresWithoutTargets()
    {
        EcsWorld world = new();
        EntityCommands commands = new();
        Entity arrow = CreateArrow(world, commands, new Vector2(5, 5), Vector2.UnitX, 0.01f);
        ProjectileSystem system = new(OpenTerrain()) { Buffer = commands };
        system.Update(world, 1f / 60f);
        commands.Apply(world);
        TestAssert.True(!world.IsAlive(arrow), "projectile expires without a target");
    }

    private static void Projectile_DamagesEnemyButNotPlayerFactionTarget()
    {
        EcsWorld world = new();
        EntityCommands commands = new();
        Entity enemy = commands.Create(world);
        commands.Add(enemy, new Position(new Vector2(2.35f, 5f)));
        commands.Add(enemy, new MonsterState { Radius = 0.35f });
        commands.Add(enemy, new Health(1));
        commands.Add(enemy, new Faction { Team = Team.Enemy });
        Entity companion = commands.Create(world);
        commands.Add(companion, new Position(new Vector2(2.35f, 6f)));
        commands.Add(companion, new MonsterState { Radius = 0.35f });
        commands.Add(companion, new Health(1));
        commands.Add(companion, new Faction { Team = Team.Player });
        commands.Apply(world);
        commands.Clear();
        Entity arrow = CreateArrow(world, commands, new Vector2(2, 5), Vector2.UnitX, 1f);
        ProjectileSystem system = new(OpenTerrain()) { Buffer = commands };
        system.Update(world, 1f / 60f);
        commands.Apply(world);
        TestAssert.True(!world.IsAlive(arrow) && !world.IsAlive(enemy) && world.IsAlive(companion), "projectile targets enemy faction only");
    }

    private static void Monsters_MoveDeterministicallyOnWalkableTerrain()
    {
        TerrainSurface terrain = OpenTerrain();
        EcsWorld world = new();
        EntityCommands commands = new();
        Entity monster = commands.Create(world);
        commands.Add(monster, new Position(new Vector2(8, 8)));
        commands.Add(monster, new MonsterState { Speed = 1f, Radius = 0.35f, WanderTarget = new Vector2(8, 8) });
        commands.Add(monster, new Faction { Team = Team.Enemy });
        commands.Apply(world);
        MonsterMovementSystem system = new(terrain);
        Vector2 before = world.Get<Position>(monster).Value;
        system.Update(world, 1f / 60f);
        Vector2 after = world.Get<Position>(monster).Value;
        TestAssert.True(after != before && terrain.CanOccupy(after, 0.35f), "enemy monsters move on walkable terrain");
    }

    private static void EnemyExtraction_UsesNeutralTextureAndGreenVariants()
    {
        TerrainSurface terrain = OpenTerrain();
        IsometricCamera camera = new(new Vector2(320, 240));
        SpritePacket[] sprites = new SpritePacket[6];
        Vector4[] colors =
        [
            new Vector4(0.08f, 0.28f, 0.10f, 1f),
            new Vector4(0.16f, 0.42f, 0.18f, 1f),
            new Vector4(0.10f, 0.34f, 0.12f, 1f),
        ];

        foreach (Vector4 color in colors)
        {
            int written = SpriteExtraction.WriteEntity(terrain, camera, sprites, 0, Vector2.One, new Vector2(32, 40), default, 0f, color);
            TestAssert.True(written == 2, "enemy extraction writes border and fill");
            TestAssert.True(sprites[1].Texture.Value == 0, "enemy extraction uses neutral texture");
            TestAssert.True(sprites[1].Color == color && sprites[1].BottomColor == color, "default extraction remains solid-colored");
        }

        Vector4 topColor = new(0.05f, 0.18f, 0.06f, 1f);
        Vector4 bottomColor = new(0.16f, 0.42f, 0.18f, 1f);
        RenderItem enemy = new(Vector2.One, new Vector2(36, 44), default, topColor)
        {
            BottomColor = bottomColor
        };
        int gradientWritten = SpriteExtraction.WriteEntity(terrain, camera, sprites, 0, in enemy);
        TestAssert.True(gradientWritten == 2 && sprites[1].Color == topColor && sprites[1].BottomColor == bottomColor, "enemy extraction preserves dark-top light-bottom gradient");
    }
}
