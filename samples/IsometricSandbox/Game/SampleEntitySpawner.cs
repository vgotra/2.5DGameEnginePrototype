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

    public HeroDefinition CreateHeroDefinition()
        => HeroDefinitions.Create(HeroIds.Rogue) with
        {
            Texture = _textures.Player,
            SpriteSize = new Vector2(44, SampleConfig.PlayerSpriteHeight),
            ColliderRadius = SampleConfig.PlayerRadius,
            Color = Vector4.One
        };

    public void RegisterHeroDefinition()
        => _world.Catalog.Register(HeroIds.Rogue, CreateHeroDefinition());

    public Entity SpawnHero(Vector2 position)
    {
        Entity entity = _world.SpawnHero(CreateHeroDefinition() with { Position = position });
        return AttachHeroComponents(entity, position);
    }

    public Entity SpawnHero(HeroId id, MapLocation location)
    {
        Entity entity = _world.SpawnHero(id, location);
        return AttachHeroComponents(entity, location.Position);
    }

    public Entity SpawnAbilityProjectile(in ProjectileDefinition definition)
        => _world.SpawnProjectile(in definition);

    private Entity AttachHeroComponents(Entity entity, Vector2 position)
    {
        _world.Commands.Add(entity, PlayerState.At(position));
        _world.Commands.Add(entity, new AbilityState());
        return entity;
    }
}
