using Engine.App;
using Engine.Ecs.Sparse;
using AppWorld = Engine.App.World;
using System.Numerics;

namespace IsometricSandbox.Game;

public enum GameProgressionStage : byte { Village, GoblinForest, Returning, Complete }

public sealed class GameProgression
{
    private static readonly Vector2 CompanionSideOffset = new(1.25f, 0f);
    private readonly AppWorld _world;
    private readonly TerrainSurface _terrain;
    private readonly QuestDefinition _quest = GameContent.CreateGoblinProblem();
    private readonly Entity[] _enemies = new Entity[3];
    private Entity _player;
    private Entity _companion;
    private int _tick;
    private int _nextCombatTick = 30;
    private int _enemyCount;
    private readonly Vector2[] _playerRoute = new Vector2[64];
    private readonly Vector2[] _companionRoute = new Vector2[64];
    private Vector2 _playerRouteTarget, _companionRouteTarget;
    private int _playerRouteLength, _companionRouteLength, _playerRouteIndex, _companionRouteIndex;
    private readonly PresentationReaction[] _navigationReactions = new PresentationReaction[8];
    private int _navigationReactionCount;
    private readonly PresentationReaction[] _combatReactions = new PresentationReaction[8];
    private int _combatReactionCount;
    private Inventory _playerInventory;
    private Equipment _playerEquipment;
    private SkillKnowledge _playerSkills;
    private SkillLoadout _playerSkillLoadout;
    private CombatStats _playerCombatStats;
    private NavigationResult _previousPlayerResult, _previousCompanionResult;

    public GameProgression(AppWorld world, TerrainSurface terrain) { _world = world; _terrain = terrain; }

    public GameProgressionStage Stage { get; private set; } = GameProgressionStage.Village;
    public QuestState Quest;
    public Inventory PlayerInventory => _playerInventory;
    public Equipment PlayerEquipment => _playerEquipment;
    public SkillKnowledge PlayerSkills => _playerSkills;
    public SkillLoadout PlayerSkillLoadout => _playerSkillLoadout;
    public CombatStats PlayerCombatStats => _playerCombatStats;
    public ItemId EquippedItem { get; private set; }
    public int Gold { get; private set; }
    public bool CompanionActive => _companion.IsValid;
    public AiIntent CompanionIntent { get; private set; }
    public NavigationResult PlayerNavigationResult { get; private set; }
    public NavigationResult CompanionNavigationResult { get; private set; }
    public ReadOnlySpan<PresentationReaction> NavigationReactions => _navigationReactions.AsSpan(0, _navigationReactionCount);
    public ReadOnlySpan<PresentationReaction> CombatReactions => _combatReactions.AsSpan(0, _combatReactionCount);

    public void Start(Entity player)
    {
        _player = player;
        Quest = default;
        Stage = GameProgressionStage.Village;
        _tick = 0;
        EquippedItem = default;
        Gold = 0;
        _playerRouteLength = _companionRouteLength = 0;
        PlayerNavigationResult = CompanionNavigationResult = NavigationResult.None;
        _previousPlayerResult = _previousCompanionResult = NavigationResult.None;
        _navigationReactionCount = 0;
        _combatReactionCount = 0;
        _playerInventory = default;
        _playerEquipment = default;
        _playerSkills = default;
        _playerSkillLoadout = default;
        _playerCombatStats = default;
        InitializePlayerLoadout();
    }

    private void InitializePlayerLoadout()
    {
        GameplayCatalog catalog = _world.Catalog;
        catalog.RegisterDefaultItems();
        catalog.RegisterDefaultSkills();
        InventorySystem.TryAdd(ref _playerInventory, GameContent.GoblinSlayerBow, Inventory.Capacity);
        EquipmentSystem.TryEquip(ref _playerEquipment, ref _playerInventory, GameContent.GoblinSlayerBow, in catalog);
        if (catalog.TryGet(SkillIds.PowerShot, out GameplaySkillDefinition powerShot)) _playerSkills.Learn(in powerShot);
        if (catalog.TryGet(SkillIds.PoisonArrow, out GameplaySkillDefinition poisonArrow)) _playerSkills.Learn(in poisonArrow);
        _playerSkillLoadout.AssignSkill(0, SkillIds.PowerShot, in _playerSkills);
        _playerSkillLoadout.AssignSkill(1, SkillIds.PoisonArrow, in _playerSkills);
        Attributes attributes = _world.EcsWorld.Get<Attributes>(_player);
        _playerCombatStats = StatSystem.Calculate(in attributes, in _playerEquipment, in catalog);
    }

    public void Tick(in PlayerCommand command)
    {
        _tick++;
        CharacterIntent intent = command.IsPressed(InputAction.PrimaryAttack)
            ? new CharacterIntent(CharacterIntentKind.MoveTo, default, command.Aim, default, default, default)
            : PlayerIntentMapper.FromCommand(in command, default);
        if (_world.EcsWorld.IsAlive(_player) && intent.Kind == CharacterIntentKind.Move)
        {
            ref CharacterMovement movement = ref _world.EcsWorld.Get<CharacterMovement>(_player);
            NavigationRuntime.Apply(ref movement, in intent);
            Position position = _world.EcsWorld.Get<Position>(_player);
            EnsureRoute(_terrain, position.Value, movement.Destination, SampleConfig.PlayerRadius, _playerRoute, ref _playerRouteTarget, ref _playerRouteLength, ref _playerRouteIndex);
            CharacterMovement stepMovement = movement; if (_playerRouteLength > _playerRouteIndex + 1) stepMovement.Destination = _playerRoute[++_playerRouteIndex];
            Vector2 desired = NavigationRuntime.Step(position.Value, in stepMovement, 3f, 1f / 60f);
            position.Value = _terrain.ResolveMove(position.Value, desired, SampleConfig.PlayerRadius);
            PlayerNavigationResult = position.Value == movement.Destination ? NavigationResult.Arrived : _playerRouteLength == 0 ? NavigationResult.Blocked : NavigationResult.Moving;
            PublishNavigationReaction(_player, PlayerNavigationResult, ref _previousPlayerResult);
            _world.EcsWorld.Get<Position>(_player) = position;
        }
        switch (Stage)
        {
            case GameProgressionStage.Village when _tick == 1 || intent.Kind == CharacterIntentKind.Interact:
                QuestRuntime.Activate(ref Quest, in _quest);
                _world.Map.Unlock(new WorldMapLocationId("goblin-forest"));
                TravelToForest();
                break;
            case GameProgressionStage.GoblinForest:
                UpdateCompanionIntent();
                break;
            case GameProgressionStage.Returning when _tick >= _nextCombatTick:
                CompleteReturn();
                break;
        }
    }

    private void UpdateCompanionIntent()
    {
        if (!_companion.IsValid || !_world.EcsWorld.IsAlive(_companion) || !_world.EcsWorld.IsAlive(_player)) return;
        Vector2 companionPosition = _world.EcsWorld.Get<Position>(_companion).Value;
        Vector2 playerPosition = _world.EcsWorld.Get<Position>(_player).Value;
        AiState state = new() { Definition = new AiDefinition(AiIntentKind.Follow, 8f, 0.2f, new SkillId("cleric-heal")) };
        float healthPercent = 1f;
        if (_world.EcsWorld.Has<Health>(_player)) healthPercent = Math.Clamp(_world.EcsWorld.Get<Health>(_player).Value / 100f, 0f, 1f);
        AiRuntime.Evaluate(ref state, companionPosition, playerPosition, healthPercent, _player, default);
        CompanionIntent = state.Intent;
        if (state.Intent.Kind == AiIntentKind.Follow)
        {
            ref CharacterMovement movement = ref _world.EcsWorld.Get<CharacterMovement>(_companion);
            CharacterIntent follow = new(CharacterIntentKind.MoveTo, default, playerPosition + CompanionSideOffset, default, default, default);
            NavigationRuntime.Apply(ref movement, in follow);
            Position position = _world.EcsWorld.Get<Position>(_companion);
            EnsureRoute(_terrain, position.Value, movement.Destination, 0.35f, _companionRoute, ref _companionRouteTarget, ref _companionRouteLength, ref _companionRouteIndex);
            CharacterMovement stepMovement = movement; if (_companionRouteLength > _companionRouteIndex + 1) stepMovement.Destination = _companionRoute[++_companionRouteIndex];
            Vector2 desired = NavigationRuntime.Step(position.Value, in stepMovement, 1.8f, 1f / 60f);
            position.Value = _terrain.ResolveMove(position.Value, desired, 0.35f);
            CompanionNavigationResult = position.Value == movement.Destination ? NavigationResult.Arrived : _companionRouteLength == 0 ? NavigationResult.Blocked : NavigationResult.Moving;
            PublishNavigationReaction(_companion, CompanionNavigationResult, ref _previousCompanionResult);
            _world.EcsWorld.Get<Position>(_companion) = position;
        }
    }

    private void PublishNavigationReaction(Entity source, NavigationResult result, ref NavigationResult previous)
    {
        if (result == previous || result is NavigationResult.None or NavigationResult.Moving) return;
        previous = result;
        if (_navigationReactionCount >= _navigationReactions.Length) return;
        byte kind = result == NavigationResult.Arrived ? (byte)20 : (byte)21;
        _navigationReactions[_navigationReactionCount++] = new PresentationReaction(kind, default, default, source, default, 0);
    }

    private static void EnsureRoute(TerrainSurface terrain, Vector2 position, Vector2 target, float radius, Vector2[] route, ref Vector2 cachedTarget, ref int length, ref int index)
    {
        if (length > 0 && cachedTarget == target && index < length && Vector2.DistanceSquared(position, route[index]) < 0.36f) return;
        cachedTarget = target; index = 0; length = Navigation.BuildGridPath(terrain, position, target, radius, route);
    }

    private void TravelToForest()
    {
        if (!_world.Map.CanTravel(new WorldMapLocationId("village"), new WorldMapLocationId("goblin-forest"))) return;
        _world.ChangeScene(GameContent.GoblinForestScene.Value);
        Scene scene = _world.ActiveScene!;
        _enemies[0] = SpawnEnemy(scene, GameContent.GoblinWarrior, "warrior-camp");
        _enemies[1] = SpawnEnemy(scene, GameContent.GoblinArcher, "archer-camp");
        _enemies[2] = SpawnEnemy(scene, GameContent.GoblinShaman, "shaman-camp");
        _companion = _world.SpawnNpc(new NpcDefinition(3, scene.Map.Resolve("cleric-companion").Position, 1.4f, 0.35f, 20, default, new System.Numerics.Vector2(34, 44), System.Numerics.Vector4.One));
        _world.Commands.Add(_companion, new Faction { Team = Team.Player });
        _world.Commands.Add(_companion, new Companion { Owner = _player });
        _world.Commands.Add(_companion, new CharacterMovement { Mode = CharacterIntentKind.Stop });
        _world.ApplyCommands();
        _enemyCount = 3;
        Stage = GameProgressionStage.GoblinForest;
    }

    private Entity SpawnEnemy(Scene scene, EnemyId id, string marker)
    {
        Entity entity = scene.SpawnEnemy(id, marker);
        MapLocation location = scene.Map.Resolve(marker);
        if (_world.Catalog.TryGet(id, out MonsterDefinition definition))
            _world.Commands.Add(entity, new MonsterState { Type = definition.Type, Speed = definition.Speed, Radius = definition.ColliderRadius, WanderTarget = location.Position });
        _world.Commands.Add(entity, new Faction { Team = Team.Enemy });
        return entity;
    }

    public void RecordProjectileKill()
    {
        if (Stage != GameProgressionStage.GoblinForest) return;
        QuestRuntime.RecordKill(ref Quest, GameContent.GoblinWarrior, in _quest);
        _enemyCount--;
        AddCombatReaction(new PresentationReaction(30, new VfxId("arrow-impact"), new SoundId("arrow-impact"), _player, default, 1));
        if (_enemyCount != 0) return;
        EquippedItem = _quest.Reward.Item;
        Gold += _quest.Reward.Gold;
        Stage = GameProgressionStage.Returning;
        _nextCombatTick = _tick + 30;
    }

    private void AddCombatReaction(in PresentationReaction reaction)
    {
        if (_combatReactionCount < _combatReactions.Length) _combatReactions[_combatReactionCount++] = reaction;
    }

    private void CompleteReturn()
    {
        _world.ChangeScene(GameContent.VillageScene.Value);
        QuestRuntime.CompleteReturn(ref Quest, GameContent.ElderMarcus, in _quest);
        Stage = GameProgressionStage.Complete;
    }
}
