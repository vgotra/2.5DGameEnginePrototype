using Engine.App;
using Engine.Ecs.Sparse;
using System.Numerics;
using IsometricSandbox.Game.Configuration;
using IsometricSandbox.Game.Runtime;
using EngineGame = Engine.App.Game;

namespace IsometricSandbox.Game.Gameplay.Progression;

public enum GameProgressionStage : byte { Village, GoblinForest, Returning, Complete }

internal sealed class GameProgression
{
    private static readonly Vector2 CompanionSideOffset = new(1.25f, 0f);
    private readonly EngineGame _game;
    private readonly SampleRuntimeBridge _runtime;
    private readonly TerrainSurface _terrain;
    private readonly QuestDefinition _quest = GameContent.CreateGoblinProblem();
    private readonly Enemy[] _enemies = new Enemy[3];
    private Hero _player = null!;
    private Npc? _companion;
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

    public GameProgression(EngineGame game, SampleRuntimeBridge runtime, TerrainSurface terrain) { _game = game; _runtime = runtime; _terrain = terrain; }

    public GameProgressionStage Stage { get; private set; } = GameProgressionStage.Village;
    public QuestState Quest;
    public Inventory PlayerInventory => _playerInventory;
    public Equipment PlayerEquipment => _playerEquipment;
    public SkillKnowledge PlayerSkills => _playerSkills;
    public SkillLoadout PlayerSkillLoadout => _playerSkillLoadout;
    public CombatStats PlayerCombatStats => _playerCombatStats;
    public ItemId EquippedItem { get; private set; }
    public int Gold { get; private set; }
    public bool CompanionActive => _companion is not null && _companion.IsAlive;
    public AiIntent CompanionIntent { get; private set; }
    public NavigationResult PlayerNavigationResult { get; private set; }
    public NavigationResult CompanionNavigationResult { get; private set; }
    public ReadOnlySpan<PresentationReaction> NavigationReactions => _navigationReactions.AsSpan(0, _navigationReactionCount);
    public ReadOnlySpan<PresentationReaction> CombatReactions => _combatReactions.AsSpan(0, _combatReactionCount);

    public void Start(Hero player)
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
        InventorySystem.TryAdd(ref _playerInventory, GameContent.GoblinSlayerBow, Inventory.Capacity);
        _runtime.TryEquipItem(ref _playerEquipment, ref _playerInventory, GameContent.GoblinSlayerBow);
        _runtime.TryLearnSkill(ref _playerSkills, SkillIds.PowerShot);
        _runtime.TryLearnSkill(ref _playerSkills, SkillIds.PoisonArrow);
        _playerSkillLoadout.AssignSkill(0, SkillIds.PowerShot, in _playerSkills);
        _playerSkillLoadout.AssignSkill(1, SkillIds.PoisonArrow, in _playerSkills);
        Attributes attributes = _runtime.GetAttributes(_player.EntityHandle);
        _playerCombatStats = _runtime.CalculateCombatStats(in attributes, in _playerEquipment);
    }

    public void Tick(in PlayerCommand command)
    {
        _tick++;
        CharacterIntent intent = command.IsPressed(InputAction.PrimaryAttack)
            ? new CharacterIntent(CharacterIntentKind.MoveTo, default, command.Aim, default, default, default)
            : PlayerIntentMapper.FromCommand(in command, default);
        Entity playerEntity = _player.EntityHandle;
        if (_runtime.IsAlive(playerEntity) && intent.Kind == CharacterIntentKind.Move)
        {
            ref CharacterMovement movement = ref _runtime.GetMovement(playerEntity);
            NavigationRuntime.Apply(ref movement, in intent);
            Position position = new(_runtime.TryGetPosition(playerEntity, out Vector2 currentPosition) ? currentPosition : default);
            EnsureRoute(_terrain, position.Value, movement.Destination, SampleConfig.PlayerRadius, _playerRoute, ref _playerRouteTarget, ref _playerRouteLength, ref _playerRouteIndex);
            CharacterMovement stepMovement = movement; if (_playerRouteLength > _playerRouteIndex + 1) stepMovement.Destination = _playerRoute[++_playerRouteIndex];
            Vector2 desired = NavigationRuntime.Step(position.Value, in stepMovement, 3f, 1f / 60f);
            position.Value = _terrain.ResolveMove(position.Value, desired, SampleConfig.PlayerRadius);
            PlayerNavigationResult = position.Value == movement.Destination ? NavigationResult.Arrived : _playerRouteLength == 0 ? NavigationResult.Blocked : NavigationResult.Moving;
            PublishNavigationReaction(playerEntity, PlayerNavigationResult, ref _previousPlayerResult);
            _runtime.SetPosition(playerEntity, position);
        }
        switch (Stage)
        {
            case GameProgressionStage.Village when _tick == 1 || intent.Kind == CharacterIntentKind.Interact:
                QuestRuntime.Activate(ref Quest, in _quest);
                _game.WorldMap!.Unlock(new WorldMapLocationId("goblin-forest"));
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
        if (_companion is null || !_companion.IsAlive) return;
        Entity companionEntity = _companion.EntityHandle;
        Entity playerEntity = _player.EntityHandle;
        if (!_runtime.IsAlive(companionEntity) || !_runtime.IsAlive(playerEntity)) return;
        _runtime.TryGetPosition(companionEntity, out Vector2 companionPosition);
        _runtime.TryGetPosition(playerEntity, out Vector2 playerPosition);
        AiState state = new() { Definition = new AiDefinition(AiIntentKind.Follow, 8f, 0.2f, new SkillId("cleric-heal")) };
        float healthPercent = 1f;
        _runtime.TryGetHealthPercent(playerEntity, out healthPercent);
        AiRuntime.Evaluate(ref state, companionPosition, playerPosition, healthPercent, playerEntity, default);
        CompanionIntent = state.Intent;
        if (state.Intent.Kind == AiIntentKind.Follow)
        {
            ref CharacterMovement movement = ref _runtime.GetMovement(companionEntity);
            CharacterIntent follow = new(CharacterIntentKind.MoveTo, default, playerPosition + CompanionSideOffset, default, default, default);
            NavigationRuntime.Apply(ref movement, in follow);
            Position position = new(_runtime.TryGetPosition(companionEntity, out Vector2 currentPosition) ? currentPosition : default);
            EnsureRoute(_terrain, position.Value, movement.Destination, 0.35f, _companionRoute, ref _companionRouteTarget, ref _companionRouteLength, ref _companionRouteIndex);
            CharacterMovement stepMovement = movement; if (_companionRouteLength > _companionRouteIndex + 1) stepMovement.Destination = _companionRoute[++_companionRouteIndex];
            Vector2 desired = NavigationRuntime.Step(position.Value, in stepMovement, 1.8f, 1f / 60f);
            position.Value = _terrain.ResolveMove(position.Value, desired, 0.35f);
            CompanionNavigationResult = position.Value == movement.Destination ? NavigationResult.Arrived : _companionRouteLength == 0 ? NavigationResult.Blocked : NavigationResult.Moving;
            PublishNavigationReaction(companionEntity, CompanionNavigationResult, ref _previousCompanionResult);
            _runtime.SetPosition(companionEntity, position);
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
        if (!_game.WorldMap!.CanTravel(new WorldMapLocationId("village"), new WorldMapLocationId("goblin-forest"))) return;
        _game.StartScene(GameContent.GoblinForestScene.Value);
        Scene scene = _game.ActiveScene!;
        _enemies[0] = SpawnEnemy(scene, GameContent.GoblinWarrior, "warrior-camp");
        _enemies[1] = SpawnEnemy(scene, GameContent.GoblinArcher, "archer-camp");
        _enemies[2] = SpawnEnemy(scene, GameContent.GoblinShaman, "shaman-camp");
        _companion = scene.SpawnNpc(GameContent.ClericCompanion, "cleric-companion");
        Entity companionEntity = _companion.EntityHandle;
        _runtime.AttachCompanion(companionEntity, _player.EntityHandle);
        _runtime.ApplyCommands();
        _enemyCount = 3;
        Stage = GameProgressionStage.GoblinForest;
    }

    private Enemy SpawnEnemy(Scene scene, EnemyId id, string marker)
    {
        Enemy enemy = scene.SpawnEnemy(id, marker);
        Entity entity = enemy.EntityHandle;
        MapLocation location = scene.Map.Resolve(marker);
        if (_game.Content.TryGet(id, out MonsterDefinition definition))
            _runtime.AttachEnemy(entity, in definition, location.Position);
        return enemy;
    }

    public void RecordProjectileKill()
    {
        if (Stage != GameProgressionStage.GoblinForest) return;
        QuestRuntime.RecordKill(ref Quest, GameContent.GoblinWarrior, in _quest);
        _enemyCount--;
        AddCombatReaction(new PresentationReaction(30, new VfxId("arrow-impact"), new SoundId("arrow-impact"), _player.EntityHandle, default, 1));
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
        _game.StartScene(GameContent.VillageScene.Value);
        QuestRuntime.CompleteReturn(ref Quest, GameContent.ElderMarcus, in _quest);
        Stage = GameProgressionStage.Complete;
    }
}
