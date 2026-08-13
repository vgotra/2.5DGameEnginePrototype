using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;
using AppWorld = Engine.App.World;

namespace IsometricSandbox.Game;

public sealed class SampleEntitySpawner(AppWorld world, TextureLibrary textures)
{
    private readonly AppWorld _world = world;
    private readonly TextureLibrary _textures = textures;

    public Entity SpawnHero(Vector2 position)
    {
        Entity entity = _world.SpawnHero(new HeroDefinition(
            HeroType.Archer,
            position,
            SampleConfig.PlayerRadius,
            _textures.Player,
            new Vector2(44, 56),
            Vector4.One));
        _world.Commands.Add(entity, PlayerState.At(position));
        _world.Commands.Add(entity, new AbilityState());
        return entity;
    }

    public Entity SpawnMonster(AnimalSpecies species, Vector2 position, Vector2 size, Vector4 color)
    {
        Critter critter = CritterSystem.Create(species, position);
        Entity entity = _world.SpawnMonster(new MonsterDefinition(
            species == AnimalSpecies.Deer ? MonsterType.Deer : MonsterType.Rabbit,
            position,
            critter.Speed,
            critter.Radius,
            1,
            species == AnimalSpecies.Deer ? _textures.Deer : _textures.Rabbit,
            size,
            color));
        _world.Commands.Add(entity, critter);
        return entity;
    }

    public Entity SpawnProjectile(Vector2 position, Vector2 direction)
    {
        Entity entity = _world.SpawnProjectile(new ProjectileDefinition(
            position,
            Vector2.Normalize(direction),
            SampleConfig.ArrowSpeed,
            SampleConfig.ArrowLifetime,
            SampleConfig.ArrowRadius,
            default,
            Vector2.Zero,
            Vector4.Zero));
        _world.Commands.Add(entity, new ArrowProjectile
        {
            Direction = Vector2.Normalize(direction),
            Speed = SampleConfig.ArrowSpeed,
            Lifetime = SampleConfig.ArrowLifetime,
        });
        return entity;
    }

    public Entity SpawnItem(ItemType type, Vector2 position, TextureHandle texture, Vector2 size, Vector4 color)
        => _world.SpawnItem(new ItemDefinition(type, position, texture, size, color));

    public Entity SpawnMuzzleFlash(Vector2 position)
        => _world.SpawnEffect(new EffectDefinition(
            EffectType.MuzzleFlash,
            position,
            0.12f,
            _textures.Player,
            new Vector2(18, 18),
            new Vector4(1f, 0.8f, 0.15f, 1f)));

    public Entity SpawnAbilityProjectile(in ProjectileDefinition definition)
        => _world.SpawnProjectile(in definition);

    public Entity SpawnAbilityEffect(in EffectDefinition definition)
        => _world.SpawnEffect(in definition);
}
