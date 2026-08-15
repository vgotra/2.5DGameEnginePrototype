using System.Numerics;
using Engine.App;

namespace Engine.Tests;

internal static class GameplayFoundationTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(TypedIds_DoNotMix), TypedIds_DoNotMix),
        new(nameof(SceneMap_ResolvesStableMarkers), SceneMap_ResolvesStableMarkers),
        new(nameof(WorldMap_RequiresUnlockedConnectedLocations), WorldMap_RequiresUnlockedConnectedLocations),
        new(nameof(PlayerCommand_SeparatesPressedAndHeldActions), PlayerCommand_SeparatesPressedAndHeldActions)
        ,new(nameof(Hotbar_EnforcesTenSlotsAndResolvesSkills), Hotbar_EnforcesTenSlotsAndResolvesSkills)
        ,new(nameof(Navigation_IsDeterministic), Navigation_IsDeterministic)
        ,new(nameof(VirtualInput_MatchesPlayerCommand), VirtualInput_MatchesPlayerCommand)
        ,new(nameof(GamepadActionSets_ResolveModifierSkills), GamepadActionSets_ResolveModifierSkills)
        ,new(nameof(NavigationRuntime_UsesFixedStepMovement), NavigationRuntime_UsesFixedStepMovement)
        ,new(nameof(NavigationRuntime_ResolvesBlockedDestination), NavigationRuntime_ResolvesBlockedDestination)
        ,new(nameof(Navigation_BuildsStableGridPath), Navigation_BuildsStableGridPath)
        ,new(nameof(NavigationResults_ArePresentationNeutral), NavigationResults_ArePresentationNeutral)
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

    private static void PlayerCommand_SeparatesPressedAndHeldActions()
    {
        uint pressed = 1u << (int)InputAction.PrimaryAttack;
        uint held = pressed | 1u << (int)InputAction.Move;
        PlayerCommand command = new(Vector2.UnitX, Vector2.UnitY, pressed, held);
        TestAssert.True(command.IsPressed(InputAction.PrimaryAttack), "pressed edge is available");
        TestAssert.True(!command.IsPressed(InputAction.Move) && command.IsHeld(InputAction.Move), "held state is distinct from pressed edge");
    }
}
