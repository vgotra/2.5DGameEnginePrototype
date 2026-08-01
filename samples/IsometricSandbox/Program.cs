using System.Numerics;
using Engine.Core;
using Engine.Ecs;
using Engine.Mathematics;
using Engine.Platform;
using Engine.Platform.Win32;
using IsometricSandbox.Game;
using Engine.Rendering.Vulkan;

World world = new();
EntityId player = world.Create();
world.Storage<Position>().Add(player, new Position(4, 3));
Vector2 screen = IsometricMath.WorldToScreen(new Vector2(4, 3), 64, 32);
// DBG: Console.WriteLine($"IsometricSandbox ready: entity={player.Index}, screen=({screen.X:0},{screen.Y:0})");

if (args.Length > 0 && args[0] == "--vulkan")
{
    using Win32Window vulkanWindow = new(960, 640, "Isometric Sandbox Vulkan");
    using VulkanRenderer renderer = new(vulkanWindow.Handle, vulkanWindow.ModuleHandle);
    // DBG: Console.WriteLine($"Vulkan device and swapchain created successfully ({renderer.SwapchainImageCount} images).");
    // DBG: Console.WriteLine($"Initial clear frame result: {renderer.RenderFrame()}");
    // DBG: Console.WriteLine("Arrow keys and WASD are enabled in --window mode.");
}

if (args.Length > 0 && args[0] == "--window")
{
    using Win32Window window = new(960, 640, "Isometric Sandbox");
    Win32Input input = new();
    using Win32TileRenderer tileRenderer = new(window);
    TileMap map = new();
    Vector2 position = map.TileToWorld(2, 2);
    Vector2 facing = new(0, 1), jumpStart = position, jumpTarget = position;
    float jumpTime = 1f;
    const float jumpDuration = 0.24f;
    IsometricCamera camera = new(window.Size);
    GameClock clock = new();
    long previous = Environment.TickCount64;
    bool renderDirty = true;
    while (!window.ShouldClose && !input.IsDown(GameKey.Escape))
    {
        window.PumpEvents();
        input.Update();
        long now = Environment.TickCount64;
        clock.Advance((now - previous) / 1000.0);
        previous = now;
        Vector2 direction = new((input.IsDown(GameKey.Right) ? 1 : 0) - (input.IsDown(GameKey.Left) ? 1 : 0), (input.IsDown(GameKey.Down) ? 1 : 0) - (input.IsDown(GameKey.Up) ? 1 : 0));
        while (clock.TryConsumeFixedStep())
        {
            if (direction.LengthSquared() > 0) facing = Vector2.Normalize(direction);
            if (direction.LengthSquared() > 0 || input.WasPressed(GameKey.Space)) renderDirty = true;
            if (input.WasPressed(GameKey.Space) && jumpTime >= jumpDuration)
            {
                Vector2 candidate = position + facing * 2f;
                if (map.CanOccupy(candidate, 0.2f)) { jumpStart = position; jumpTarget = candidate; jumpTime = 0; }
            }
            if (jumpTime < jumpDuration)
            {
                jumpTime = Math.Min(jumpDuration, jumpTime + (float)GameClock.FixedStep);
                position = Vector2.Lerp(jumpStart, jumpTarget, jumpTime / jumpDuration);
                renderDirty = true;
            }
            else position = MovementSystem.Move(map, position, direction, 4, 0.2f, (float)GameClock.FixedStep);
        }
        Vector2 previousCameraPosition = camera.Position;
        camera.Follow(position, map);
        if (previousCameraPosition != camera.Position) renderDirty = true;
        float jumpProgress = Math.Clamp(jumpTime / jumpDuration, 0, 1);
        float jumpHeight = jumpProgress >= 1 ? 0 : MathF.Sin(jumpProgress * MathF.PI) * 18f;
        // DBG: if (renderDirty) { tileRenderer.Draw(map, camera, position, jumpHeight, displayedFps); renderDirty = false; }
        if (renderDirty) { tileRenderer.Draw(map, camera, position, jumpHeight); renderDirty = false; }
        Thread.Sleep(2);
    }
}

public readonly record struct Position(float X, float Y);
