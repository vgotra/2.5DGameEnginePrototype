using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Rendering;
using IsometricSandbox.Game.Configuration;
using IsometricSandbox.Game.Rendering;
using IsometricSandbox.Game.Gameplay.Components;
using IsometricSandbox.Game.Runtime;
using EngineGame = Engine.App.Game;

namespace IsometricSandbox.Game.World;

internal sealed class SampleEntitySpawner(EngineGame game, SampleRuntimeBridge runtime, TextureLibrary textures)
{
    private readonly EngineGame _game = game;
    private readonly SampleRuntimeBridge _runtime = runtime;
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
        => _game.Content.RegisterHero(HeroIds.Rogue, CreateHeroDefinition());

    public Hero SpawnHero(Vector2 position)
    {
        Hero hero = _game.ActiveScene!.SpawnHero(HeroIds.Rogue, new MapLocation(_game.ActiveScene.Map.Id, position));
        return AttachHeroComponents(hero, position);
    }

    public Hero SpawnHero(HeroId id, MapLocation location)
    {
        Hero hero = _game.ActiveScene!.SpawnHero(id, location);
        return AttachHeroComponents(hero, location.Position);
    }

    public Entity SpawnAbilityProjectile(in ProjectileDefinition definition)
        => _runtime.SpawnProjectile(in definition);

    private Hero AttachHeroComponents(Hero hero, Vector2 position)
    {
        _runtime.AttachHero(hero, position);
        return hero;
    }
}
