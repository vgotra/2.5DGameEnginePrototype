using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Threading;
using Engine.Rendering;
using IsometricSandbox.Game.Gameplay.Components;
using IsometricSandbox.Game.Gameplay.Systems;
using IsometricSandbox.Game.Rendering;
using IsometricSandbox.Game.Configuration;
using AppWorld = Engine.App.World;

namespace IsometricSandbox.Game.Runtime;

internal sealed class SampleRuntimeBridge(AppWorld world) : IGameRuntimeBridge
{
    private readonly AppWorld _world = world;
    private FrameScheduler? _scheduler;
    private Action<float>? _presentationExtractor;

    public EntityCommands Commands => _world.Commands;

    public void ApplyCommands() => _world.ApplyCommands();

    public void RunFixedStep(float deltaSeconds)
    {
        if (_scheduler is not null) _scheduler.Run(_world.EcsWorld, deltaSeconds);
    }

    public void ExtractPresentation(float interpolationAlpha)
        => _presentationExtractor?.Invoke(interpolationAlpha);

    public void ConfigureScheduler(FrameScheduler scheduler) => _scheduler = scheduler;

    public void ConfigurePresentation(Action<float> extractor)
        => _presentationExtractor = extractor;

    public bool IsAlive(Entity entity) => _world.IsEntityAlive(entity);

    public bool TryGetPosition(Entity entity, out Vector2 position)
    {
        if (!_world.EcsWorld.IsAlive(entity) || !_world.EcsWorld.Has<Position>(entity))
        {
            position = default;
            return false;
        }

        position = _world.EcsWorld.Get<Position>(entity).Value;
        return true;
    }

    public ref CharacterMovement GetMovement(Entity entity)
        => ref _world.EcsWorld.Get<CharacterMovement>(entity);

    public void SetPosition(Entity entity, Position position)
        => _world.EcsWorld.Get<Position>(entity) = position;

    public bool TryGetHealthPercent(Entity entity, out float healthPercent)
    {
        if (!_world.EcsWorld.Has<Health>(entity))
        {
            healthPercent = 1f;
            return false;
        }

        healthPercent = Math.Clamp(_world.EcsWorld.Get<Health>(entity).Value / 100f, 0f, 1f);
        return true;
    }

    public Attributes GetAttributes(Entity entity)
        => _world.EcsWorld.Get<Attributes>(entity);

    public bool TryEquipItem(ref Equipment equipment, ref Inventory inventory, ItemId item)
    {
        GameplayCatalog catalog = _world.Catalog;
        return EquipmentSystem.TryEquip(ref equipment, ref inventory, item, in catalog);
    }

    public bool TryLearnSkill(ref SkillKnowledge knowledge, SkillId skill)
        => _world.Catalog.TryGet(skill, out GameplaySkillDefinition definition) && knowledge.Learn(in definition);

    public CombatStats CalculateCombatStats(in Attributes attributes, in Equipment equipment)
    {
        GameplayCatalog catalog = _world.Catalog;
        return StatSystem.Calculate(in attributes, in equipment, in catalog);
    }

    public void AttachCompanion(Entity entity, Entity owner)
    {
        _world.Commands.Add(entity, new Faction { Team = Team.Player });
        _world.Commands.Add(entity, new Companion { Owner = owner });
        _world.Commands.Add(entity, new CharacterMovement { Mode = CharacterIntentKind.Stop });
    }

    public void AttachEnemy(Entity entity, in MonsterDefinition definition, Vector2 wanderTarget)
    {
        _world.Commands.Add(entity, new MonsterState { Type = definition.Type, Speed = definition.Speed, Radius = definition.ColliderRadius, WanderTarget = wanderTarget });
        _world.Commands.Add(entity, new Faction { Team = Team.Enemy });
    }

    public void SetPlayerAim(Entity entity, Vector2 aimTarget)
    {
        if (!_world.EcsWorld.IsAlive(entity)) return;
        ref PlayerState state = ref _world.EcsWorld.Get<PlayerState>(entity);
        state.AimTarget = aimTarget;
        state.PendingShot = true;
    }

    public void AttachPlayerMovement(Entity entity)
        => _world.Commands.Add(entity, new CharacterMovement { Mode = CharacterIntentKind.Stop });

    public void AttachProjectile(Entity entity, in ProjectileDefinition definition)
        => _world.Commands.Add(entity, new ArrowProjectile { Direction = definition.Direction, Speed = definition.Speed, Lifetime = definition.Lifetime });

    public void ConfigureProjectileSystems(ProjectileSystem projectiles, LifetimeSystem lifetimes)
    {
        projectiles.Buffer = _world.Commands;
        lifetimes.Buffer = _world.Commands;
    }

    public void ResetPlayer(Entity entity, Vector2 position)
    {
        _world.EcsWorld.Get<Position>(entity).Value = position;
        _world.EcsWorld.Get<Velocity>(entity).Value = Vector2.Zero;
        _world.EcsWorld.SetComponent(entity, PlayerState.At(position));
    }

    public ref AbilityState GetAbilityState(Entity entity)
        => ref _world.EcsWorld.Get<AbilityState>(entity);

    public bool TryConsumePlayerShot(Entity entity, out Vector2 aimTarget)
    {
        ref PlayerState state = ref _world.EcsWorld.Get<PlayerState>(entity);
        if (!state.PendingShot)
        {
            aimTarget = default;
            return false;
        }

        state.PendingShot = false;
        aimTarget = state.AimTarget;
        return true;
    }

    public int ProjectileCount()
        => _world.EcsWorld.Query<Position, ArrowProjectile>().Count;

    public void ClearProjectiles()
    {
        ArrowCollectBody body = new() { Buffer = _world.Commands };
        _world.EcsWorld.Query<ArrowProjectile>().ForEach(ref body);
    }

    public void RunFixedStep(FrameScheduler scheduler, float deltaSeconds)
        => scheduler.Run(_world.EcsWorld, deltaSeconds);

    public void BeginPresentationStep(PresentationPositionHistory history)
        => history.BeginStep(_world.EcsWorld);

    public void EndPresentationStep(PresentationPositionHistory history)
        => history.EndStep(_world.EcsWorld);

    public bool TryGetInterpolated(PresentationPositionHistory history, Entity entity, double alpha, out Vector2 position)
        => history.TryGetInterpolated(entity, alpha, out position);

    public int ExtractEntities(
        TerrainSurface grid,
        IsometricCamera camera,
        SpritePacket[] sprites,
        int written,
        Vector2 playerWorld,
        Vector2 playerSize,
        TextureHandle playerTexture,
        Vector4 playerColor,
        Entity player,
        PresentationPositionHistory history,
        double interpolationAlpha,
        VfxPool vfxPool,
        RenderItem[] vfxItems)
    {
        ref PlayerState playerState = ref _world.EcsWorld.Get<PlayerState>(player);
        written = SpriteExtraction.WriteEntity(grid, camera, sprites, written, playerWorld, playerSize, playerTexture, playerState.JumpHeight, playerColor);
        EntityRenderBody entityBody = new()
        {
            Grid = grid,
            Camera = camera,
            Sprites = sprites,
            Written = written,
            ExcludedEntity = player,
            History = history,
            InterpolationAlpha = interpolationAlpha
        };
        _world.EcsWorld.Query<Position, Renderable>().ForEach(ref entityBody);
        ArrowRenderBody arrowBody = new()
        {
            Grid = grid,
            Camera = camera,
            Sprites = sprites,
            Written = entityBody.Written,
            History = history,
            InterpolationAlpha = interpolationAlpha
        };
        _world.EcsWorld.Query<Position, ArrowProjectile>().ForEach(ref arrowBody);
        int vfxCount = vfxPool.Extract(vfxItems, new Vector2(0f, -SampleConfig.PlayerSpriteHeight * 0.5f));
        for (int index = 0; index < vfxCount; index++)
            arrowBody.Written = SpriteExtraction.WriteEntity(grid, camera, sprites, arrowBody.Written, in vfxItems[index]);
        return arrowBody.Written;
    }

    public void AttachHero(Hero hero, Vector2 position)
    {
        Entity entity = hero.EntityHandle;
        _world.Commands.Add(entity, PlayerState.At(position));
        _world.Commands.Add(entity, new AbilityState());
    }

    public Entity SpawnProjectile(in ProjectileDefinition definition)
        => _world.SpawnProjectile(in definition);
}
