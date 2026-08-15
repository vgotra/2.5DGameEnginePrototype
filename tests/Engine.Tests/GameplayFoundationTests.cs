using System.Numerics;
using Engine.App;
using SparseEntity = Engine.Ecs.Sparse.Entity;
using Engine.Platform;

namespace Engine.Tests;

internal static class GameplayFoundationTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(TypedIds_DoNotMix), TypedIds_DoNotMix),
        new(nameof(SceneMap_ResolvesStableMarkers), SceneMap_ResolvesStableMarkers),
        new(nameof(WorldMap_RequiresUnlockedConnectedLocations), WorldMap_RequiresUnlockedConnectedLocations),
        new(nameof(WorldMap_TracksCurrentLocationAndTravel), WorldMap_TracksCurrentLocationAndTravel),
        new(nameof(PlayerCommand_SeparatesPressedAndHeldActions), PlayerCommand_SeparatesPressedAndHeldActions)
        ,new(nameof(InputBindings_DefaultsAndRebinding), InputBindings_DefaultsAndRebinding)
        ,new(nameof(InputBindings_MapCustomMouseAndKeyboard), InputBindings_MapCustomMouseAndKeyboard)
        ,new(nameof(Hotbar_EnforcesTenSlotsAndResolvesSkills), Hotbar_EnforcesTenSlotsAndResolvesSkills)
        ,new(nameof(Navigation_IsDeterministic), Navigation_IsDeterministic)
        ,new(nameof(VirtualInput_MatchesPlayerCommand), VirtualInput_MatchesPlayerCommand)
        ,new(nameof(GamepadActionSets_ResolveModifierSkills), GamepadActionSets_ResolveModifierSkills)
        ,new(nameof(NavigationRuntime_UsesFixedStepMovement), NavigationRuntime_UsesFixedStepMovement)
        ,new(nameof(NavigationRuntime_ResolvesBlockedDestination), NavigationRuntime_ResolvesBlockedDestination)
        ,new(nameof(Navigation_BuildsStableGridPath), Navigation_BuildsStableGridPath)
        ,new(nameof(NavigationResults_ArePresentationNeutral), NavigationResults_ArePresentationNeutral)
        ,new(nameof(NpcDefinitions_AttachDeclaredCapabilities), NpcDefinitions_AttachDeclaredCapabilities)
    ];

    private static void TypedIds_DoNotMix()
    {
        HeroId hero = HeroIds.Rogue;
        EnemyId enemy = new("rogue");
        TestAssert.True(hero.Value == enemy.Value, "IDs may share text");
        TestAssert.True(hero != (HeroId)(object)new HeroId("paladin"), "typed IDs preserve identity");
    }

    private static void Hotbar_EnforcesTenSlotsAndResolvesSkills()
    {
        Hotbar hotbar = default; SkillId skill = new("poison-arrow");
        TestAssert.True(hotbar.AssignSkill(9, skill) && hotbar.GetSkill(9) == skill, "hotbar stores slot ten");
        TestAssert.True(!hotbar.AssignSkill(10, skill), "hotbar rejects slot eleven");
    }

    private static void Navigation_IsDeterministic()
    {
        Span<Vector2> first = stackalloc Vector2[8]; Span<Vector2> second = stackalloc Vector2[8];
        int a = Navigation.BuildDirectPath(Vector2.Zero, new Vector2(2, 2), first); int b = Navigation.BuildDirectPath(Vector2.Zero, new Vector2(2, 2), second);
        TestAssert.True(a == b && first[..a].SequenceEqual(second[..b]), "navigation path is deterministic");
    }

    private static void VirtualInput_MatchesPlayerCommand()
    {
        VirtualInput input = default; input.SetMove(Vector2.UnitX); input.Press(InputAction.Skill2);
        PlayerCommand command = input.Command;
        TestAssert.True(command.Move == Vector2.UnitX && command.IsPressed(InputAction.Skill2) && command.IsHeld(InputAction.Skill2), "virtual input produces the shared action state");
        input.ConsumeEdges();
        TestAssert.True(!input.Command.IsPressed(InputAction.Skill2) && input.Command.IsHeld(InputAction.Skill2), "virtual input edges are consumed independently");
    }

    private static void GamepadActionSets_ResolveModifierSkills()
    {
        TestAssert.True(ActionSetResolver.ResolveGamepad(3, false) == InputAction.Skill1, "gamepad face action maps to skill one");
        TestAssert.True(ActionSetResolver.ResolveGamepad(3, true) == InputAction.Skill5, "modifier maps the same face action to skill five");
    }

    private static void NavigationRuntime_UsesFixedStepMovement()
    {
        CharacterMovement movement = default; CharacterIntent intent = new(CharacterIntentKind.MoveTo, default, new Vector2(1, 0), default, default, default);
        NavigationRuntime.Apply(ref movement, in intent);
        Vector2 first = NavigationRuntime.Step(Vector2.Zero, in movement, 2f, 0.25f);
        Vector2 second = NavigationRuntime.Step(first, in movement, 2f, 0.25f);
        TestAssert.True(first.X == 0.5f && second.X == 1f, "navigation movement uses deterministic fixed-step distance");
    }

    private static void NavigationRuntime_ResolvesBlockedDestination()
    {
        TerrainSurface terrain = new(4, 4, 1);
        terrain.SetTile(1, 0, TileType.Wall);
        Vector2 resolved = terrain.ResolveMove(Vector2.Zero, new Vector2(1, 0), 0.2f);
        TestAssert.True(resolved == Vector2.Zero, "blocked click-to-move destination is collision-resolved");
    }

    private static void Navigation_BuildsStableGridPath()
    {
        TerrainSurface terrain = new(5, 5, 1); terrain.LoadLayout([".....", ".#...", ".#...", ".....", "....."]);
        Span<Vector2> first = stackalloc Vector2[32]; Span<Vector2> second = stackalloc Vector2[32];
        int a = Navigation.BuildGridPath(terrain, new(0.5f, 0.5f), new(2.5f, 0.5f), 0.2f, first);
        int b = Navigation.BuildGridPath(terrain, new(0.5f, 0.5f), new(2.5f, 0.5f), 0.2f, second);
        TestAssert.True(a > 0 && a == b && first[..a].SequenceEqual(second[..b]), "grid navigation is stable around obstacles");
    }

    private static void NavigationResults_ArePresentationNeutral()
    {
        PresentationReaction reaction = new(20, default, default, default, default, 0);
        TestAssert.True(reaction.Kind == 20 && reaction.Vfx.Value is null, "navigation reactions remain renderer-neutral");
    }

    private static void NpcDefinitions_AttachDeclaredCapabilities()
    {
        GameplayCatalog catalog = new();
        catalog.RegisterDefaultNpcs();
        TestAssert.True(catalog.TryGet(GameContent.ElderMarcus, out NpcDefinition elder), "elder definition is registered");
        TestAssert.True((elder.Capabilities.Flags & NpcCapability.QuestGiver) != 0 && elder.Quest == GameContent.GoblinProblem, "elder exposes quest capability");
        TestAssert.True(catalog.TryGet(GameContent.VillageBlacksmith, out NpcDefinition blacksmith) && (blacksmith.Capabilities.Flags & NpcCapability.Merchant) != 0, "blacksmith exposes merchant capability");
        TestGame game = new();
        World world = game.Create("npc-capabilities");
        SparseEntity entity = world.SpawnNpc(elder);
        TestAssert.True(!world.EcsWorld.IsAlive(entity), "npc spawn remains deferred");
        world.ApplyCommands();
        TestAssert.True(world.EcsWorld.Has<NpcCapabilities>(entity), "declared NPC capabilities attach after command application");
        TestAssert.True(world.EcsWorld.Get<NpcCapabilities>(entity).Flags == (NpcCapability.Dialogue | NpcCapability.QuestGiver), "NPC capability flags remain exact");
    }

    private static void SceneMap_ResolvesStableMarkers()
    {
        MapId mapId = new("village");
        SceneMap map = new(mapId);
        map.AddMarker("elder", new Vector2(4, 7), 1.5f);
        MapLocation location = map.Resolve("elder");
        TestAssert.True(location.Map == mapId && location.Position == new Vector2(4, 7) && location.Elevation == 1.5f, "marker resolves to map location");
    }

    private static void WorldMap_RequiresUnlockedConnectedLocations()
    {
        WorldMap map = new();
        WorldMapLocationId village = new("village");
        WorldMapLocationId forest = new("forest");
        map.Register(new WorldLocation(village, new SceneId("village-scene")));
        map.Register(new WorldLocation(forest, new SceneId("forest-scene")));
        map.Connect(village, forest);
        map.Unlock(village);
        TestAssert.True(!map.CanTravel(village, forest), "locked destinations cannot be entered");
        map.Unlock(forest);
        TestAssert.True(map.CanTravel(village, forest), "connected unlocked destinations can be entered");
    }

    private static void WorldMap_TracksCurrentLocationAndTravel()
    {
        WorldMap map = new();
        WorldMapLocationId village = new("village");
        WorldMapLocationId forest = new("forest");
        WorldMapLocationId ruins = new("ruins");
        map.Register(new WorldLocation(village, new SceneId("village"), true));
        map.Register(new WorldLocation(forest, new SceneId("forest")));
        map.Register(new WorldLocation(ruins, new SceneId("ruins")));
        map.Connect(village, forest);
        map.Connect(forest, ruins);
        TestAssert.True(map.HasCurrentLocation && map.CurrentLocation == village, "first unlocked location becomes current");
        map.Unlock(forest);
        TestAssert.True(map.CurrentLocation == village, "unlocking a later location does not change current location");
        TestAssert.True(map.TravelTo(forest) && map.CurrentLocation == forest, "connected unlocked travel updates current location");
        TestAssert.True(!map.TravelTo(village) && map.CurrentLocation == forest, "directed reverse travel is rejected");
        TestAssert.True(!map.TravelTo(new WorldMapLocationId("missing")) && map.CurrentLocation == forest, "unknown travel preserves current location");
        TestAssert.True(map.TryGetScene(forest, out SceneId scene) && scene.Value == "forest", "world location resolves scene ID");
    }

    private static void PlayerCommand_SeparatesPressedAndHeldActions()
    {
        uint pressed = 1u << (int)InputAction.PrimaryAttack;
        uint held = pressed | 1u << (int)InputAction.Move;
        PlayerCommand command = new(Vector2.UnitX, Vector2.UnitY, pressed, held);
        TestAssert.True(command.IsPressed(InputAction.PrimaryAttack), "pressed edge is available");
        TestAssert.True(!command.IsPressed(InputAction.Move) && command.IsHeld(InputAction.Move), "held state is distinct from pressed edge");
    }

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }

    private static void InputBindings_DefaultsAndRebinding()
    {
        InputBindingMap map = new();
        TestAssert.True(map.Count == 22, "default bindings preserve the gameplay map");
        ActionBinding custom = new(InputAction.PrimaryAttack, InputBindingKind.Keyboard, (int)GameKey.F);
        TestAssert.True(map.Replace(InputAction.PrimaryAttack, custom), "binding replacement succeeds");
        TestAssert.True(!map.Add(new(InputAction.Dodge, InputBindingKind.Keyboard, (int)GameKey.F)), "conflicting bindings are rejected");
        TestAssert.True(map.Remove(InputAction.PrimaryAttack), "binding removal succeeds");
        map.ResetDefaults();
        TestAssert.True(map.Count == 22, "reset restores defaults");
    }

    private static void InputBindings_MapCustomMouseAndKeyboard()
    {
        InputBindingMap map = new([]);
        TestAssert.True(map.Add(new(InputAction.PrimaryAttack, InputBindingKind.Mouse, (int)MouseButton.Right)), "mouse binding adds");
        TestAssert.True(map.Add(new(InputAction.Skill1, InputBindingKind.Keyboard, (int)GameKey.F)), "keyboard binding adds");
        FakeInput input = new() { RightMouseDown = true, RightMousePressed = true, FDown = true, FPressed = true };
        InputActionBuffer buffer = default;
        InputActionMapper.CaptureCurrent(input, ref buffer, map);
        TestAssert.True(buffer.Snapshot().IsPressed(InputAction.PrimaryAttack), "custom mouse press maps to action");
        TestAssert.True(buffer.Snapshot().IsHeld(InputAction.Skill1), "custom keyboard hold maps to action");
    }

    private sealed class FakeInput : IInputState
    {
        public bool RightMouseDown;
        public bool RightMousePressed;
        public bool FDown;
        public bool FPressed;
        public void Update() { }
        public bool IsDown(GameKey key) => key == GameKey.F && FDown;
        public bool WasPressed(GameKey key) => key == GameKey.F && FPressed;
        public bool WasReleased(GameKey key) => false;
        public bool IsMouseButtonDown(MouseButton button) => button == MouseButton.Right && RightMouseDown;
        public bool WasMouseButtonPressed(MouseButton button) => button == MouseButton.Right && RightMousePressed;
        public Vector2 MousePosition => Vector2.Zero;
        public bool IsMouseDown => false;
        public bool MousePressed => false;
    }
}
