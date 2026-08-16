using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;
using AppWorld = Engine.App.World;
using IsometricSandbox.Game.Configuration;
using IsometricSandbox.Game.Rendering;
using IsometricSandbox.Game.Gameplay.Components;

namespace IsometricSandbox.Game.World;

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

    public Hero SpawnHero(Vector2 position)
    {
        Hero hero = _world.ActiveScene!.SpawnHero(HeroIds.Rogue, new MapLocation(_world.ActiveScene.Map.Id, position));
        return AttachHeroComponents(hero, position);
    }

    public Hero SpawnHero(HeroId id, MapLocation location)
    {
        Hero hero = _world.ActiveScene!.SpawnHero(id, location);
        return AttachHeroComponents(hero, location.Position);
    }

    public Entity SpawnAbilityProjectile(in ProjectileDefinition definition)
        => _world.SpawnProjectile(in definition);

    private Hero AttachHeroComponents(Hero hero, Vector2 position)
    {
        Entity entity = hero.EntityHandle;
        _world.Commands.Add(entity, PlayerState.At(position));
        _world.Commands.Add(entity, new AbilityState());
        return hero;
    }
}
