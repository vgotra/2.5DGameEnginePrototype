using System.Numerics;
using Engine.App;
using Engine.Ecs.Sparse;
using Engine.Platform;
using IsometricSandbox.Game.Gameplay.Components;
using IsometricSandbox.Game.Gameplay.Systems;
using SparseWorld = Engine.Ecs.Sparse.World;

namespace Engine.Tests;

internal static class JumpTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Jump_ValidRequestAdvancesAndCompletes), Jump_ValidRequestAdvancesAndCompletes),
        new(nameof(Jump_BlockedTargetDoesNotStart), Jump_BlockedTargetDoesNotStart),
        new(nameof(Jump_InputLatchSurvivesUntilFixedStep), Jump_InputLatchSurvivesUntilFixedStep),
        new(nameof(Jump_HeldSpaceTriggersOnlyOnce), Jump_HeldSpaceTriggersOnlyOnce),
        new(nameof(Jump_WhileMovingStillStarts), Jump_WhileMovingStillStarts),
        new(nameof(Jump_PlayerCommandDrivesSampleMovement), Jump_PlayerCommandDrivesSampleMovement),
        new(nameof(Jump_CommandEdgeSurvivesMissedFixedStep), Jump_CommandEdgeSurvivesMissedFixedStep),
    ];

    private static void Jump_ValidRequestAdvancesAndCompletes()
    {
        TerrainSurface map = new(20, 20);
        Vector2 start = new(1.5f, 1.5f);
        Position position = new(start);
        Velocity velocity = new(Vector2.Zero);
        PlayerState state = PlayerState.At(start);
        PlayerMoveBody body = new() { Map = map, JumpRequested = true, DeltaSeconds = 1f / 60f };

        PlayerMoveBody.Execute(ref body, default, ref position, ref velocity, ref state);
        Vector2 afterStart = position.Value;
        TestAssert.True(state.IsJumping && state.JumpTime > 0 && afterStart != start, "valid jump advances in the fixed step");

        for (int i = 1; i < 30; i++)
        {
            body.JumpRequested = false;
            PlayerMoveBody.Execute(ref body, default, ref position, ref velocity, ref state);
        }

        TestAssert.True(!state.IsJumping && position.Value == state.JumpTarget, "jump reaches its target and ends");
    }

    private static void Jump_BlockedTargetDoesNotStart()
    {
        TerrainSurface map = new(20, 20);
        map.SetTile(1, 3, TileType.Wall);
        Vector2 start = new(1.5f, 1.5f);
        Position position = new(start);
        Velocity velocity = new(Vector2.Zero);
        PlayerState state = PlayerState.At(start);
        PlayerMoveBody body = new() { Map = map, JumpRequested = true, DeltaSeconds = 1f / 60f };

        PlayerMoveBody.Execute(ref body, default, ref position, ref velocity, ref state);

        TestAssert.True(!state.IsJumping && position.Value == start, "blocked jump target does not start");
    }

    private static void Jump_InputLatchSurvivesUntilFixedStep()
    {
        FakeInput input = new() { SpacePressed = true };
        TerrainSurface map = new(20, 20);
        PlayerMoveSystem system = new(map, input);
        SparseWorld world = new();
        Entity player = world.Create();
        Vector2 start = new(1.5f, 1.5f);
        world.Add(player, new Position(start));
        world.Add(player, new Velocity(Vector2.Zero));
        world.Add(player, PlayerState.At(start));
        system.Player = player;

        system.CaptureInput();
        input.SpacePressed = false;
        system.Update(world, 1f / 60f);

        TestAssert.True(world.Get<PlayerState>(player).IsJumping, "latched jump reaches the next fixed step");
    }

    private static void Jump_HeldSpaceTriggersOnlyOnce()
    {
        FakeInput input = new() { SpacePressed = true };
        TerrainSurface map = new(20, 20);
        PlayerMoveSystem system = new(map, input);
        SparseWorld world = new();
        Entity player = world.Create();
        Vector2 start = new(1.5f, 1.5f);
        world.Add(player, new Position(start));
        world.Add(player, new Velocity(Vector2.Zero));
        world.Add(player, PlayerState.At(start));
        system.Player = player;

        system.CaptureInput();
        system.Update(world, 1f / 60f);
        float firstJumpTime = world.Get<PlayerState>(player).JumpTime;
        input.SpacePressed = false;
        system.CaptureInput();
        system.Update(world, 1f / 60f);

        TestAssert.True(firstJumpTime > 0 && world.Get<PlayerState>(player).JumpTime > firstJumpTime, "held Space does not retrigger but jump continues");
    }

    private static void Jump_WhileMovingStillStarts()
    {
        FakeInput input = new() { SpacePressed = true, RightDown = true };
        TerrainSurface map = new(20, 20);
        PlayerMoveSystem system = new(map, input);
        SparseWorld world = new();
        Entity player = world.Create();
        Vector2 start = new(1.5f, 1.5f);
        world.Add(player, new Position(start));
        world.Add(player, new Velocity(Vector2.Zero));
        world.Add(player, PlayerState.At(start));
        system.Player = player;

        system.CaptureInput();
        input.SpacePressed = false;
        system.Update(world, 1f / 60f);

        TestAssert.True(world.Get<PlayerState>(player).IsJumping, "jump starts while movement is held");
    }

    private static void Jump_PlayerCommandDrivesSampleMovement()
    {
        TerrainSurface map = new(20, 20);
        PlayerMoveSystem system = new(map);
        SparseWorld world = new();
        Entity player = world.Create();
        Vector2 start = new(1.5f, 1.5f);
        world.Add(player, new Position(start));
        world.Add(player, new Velocity(Vector2.Zero));
        world.Add(player, PlayerState.At(start));
        system.Player = player;
        uint pressed = 1u << (int)InputAction.Dodge;
        system.SetCommand(new PlayerCommand(Vector2.UnitX, Vector2.Zero, pressed, pressed));

        system.Update(world, 1f / 60f);

        TestAssert.True(world.Get<PlayerState>(player).IsJumping, "sample movement consumes the shared player command");
    }

    private static void Jump_CommandEdgeSurvivesMissedFixedStep()
    {
        TerrainSurface map = new(20, 20);
        PlayerMoveSystem system = new(map);
        SparseWorld world = new();
        Entity player = world.Create();
        Vector2 start = new(1.5f, 1.5f);
        world.Add(player, new Position(start));
        world.Add(player, new Velocity(Vector2.Zero));
        world.Add(player, PlayerState.At(start));
        system.Player = player;

        uint jumpPressed = 1u << (int)InputAction.Dodge;
        system.SetCommand(new PlayerCommand(Vector2.Zero, Vector2.Zero, jumpPressed, jumpPressed));
        system.SetCommand(new PlayerCommand(Vector2.UnitX, Vector2.Zero, 0, 0));
        system.Update(world, 1f / 60f);

        TestAssert.True(world.Get<PlayerState>(player).IsJumping, "jump edge survives until the next fixed step");
    }

    private sealed class FakeInput : IInputState
    {
        public bool SpacePressed;
        public bool RightDown;
        public void Update() { }
        public bool IsDown(GameKey key) => key == GameKey.Right && RightDown;
        public bool WasPressed(GameKey key) => key == GameKey.Space && SpacePressed;
        public bool WasReleased(GameKey key) => false;
        public bool IsMouseButtonDown(MouseButton button) => false;
        public bool WasMouseButtonPressed(MouseButton button) => false;
        public Vector2 MousePosition => Vector2.Zero;
        public bool IsMouseDown => false;
        public bool MousePressed => false;
    }
}
