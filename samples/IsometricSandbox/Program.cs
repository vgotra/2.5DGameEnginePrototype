using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Platform.Win32;
using Engine.Rendering;
using IsometricSandbox.Game;
using Engine.Rendering.Vulkan;

if (args.Length > 0 && args[0] == "--vulkan")
{
    using Win32Window vulkanWindow = new(960, 640, "Isometric Sandbox Vulkan");
    Win32Input vulkanInput = new(vulkanWindow.Handle);
    using IRenderer renderer = new VulkanRenderer(vulkanWindow.Handle, vulkanWindow.ModuleHandle);
    TileMap vulkanMap = new();
    Vector2 vulkanPosition = vulkanMap.TileToWorld(2, 2);
    Vector2 vulkanFacing = new(0, 1), vulkanJumpStart = vulkanPosition, vulkanJumpTarget = vulkanPosition;
    float vulkanJumpTime = 1f;
    const float jumpDuration = 0.24f;
    IsometricCamera vulkanCamera = new(vulkanWindow.Size);
    GameClock vulkanClock = new();
    long vulkanPrevious = Environment.TickCount64;
    SpritePacket[] sprites = new SpritePacket[vulkanMap.Width * vulkanMap.Height * 2 + 2];
    while (!vulkanWindow.ShouldClose && !vulkanInput.IsDown(GameKey.Escape))
    {
        vulkanWindow.PumpEvents();
        vulkanInput.Update();
        long now = Environment.TickCount64;
        vulkanClock.Advance((now - vulkanPrevious) / 1000.0);
        vulkanPrevious = now;
        Vector2 direction = new((vulkanInput.IsDown(GameKey.Right) ? 1 : 0) - (vulkanInput.IsDown(GameKey.Left) ? 1 : 0), (vulkanInput.IsDown(GameKey.Down) ? 1 : 0) - (vulkanInput.IsDown(GameKey.Up) ? 1 : 0));
        while (vulkanClock.TryConsumeFixedStep())
        {
            if (direction.LengthSquared() > 0) vulkanFacing = Vector2.Normalize(direction);
            if (vulkanInput.WasPressed(GameKey.Space) && vulkanJumpTime >= jumpDuration)
            {
                Vector2 candidate = vulkanPosition + vulkanFacing * 2f;
                if (vulkanMap.CanOccupy(candidate, 0.2f)) { vulkanJumpStart = vulkanPosition; vulkanJumpTarget = candidate; vulkanJumpTime = 0; }
            }
            if (vulkanJumpTime < jumpDuration)
            {
                vulkanJumpTime = Math.Min(jumpDuration, vulkanJumpTime + (float)GameClock.FixedStep);
                vulkanPosition = Vector2.Lerp(vulkanJumpStart, vulkanJumpTarget, vulkanJumpTime / jumpDuration);
            }
            else vulkanPosition = MovementSystem.Move(vulkanMap, vulkanPosition, direction, 4, 0.2f, (float)GameClock.FixedStep);
        }
        vulkanCamera.Follow(vulkanPosition, vulkanMap);
        renderer.BeginFrame(vulkanWindow.Size);
        int tileCount = RenderExtractionSystem.ExtractMapSprites(vulkanMap, vulkanCamera, sprites);
        float jumpProgress = Math.Clamp(vulkanJumpTime / jumpDuration, 0, 1);
        float jumpHeight = jumpProgress >= 1 ? 0 : MathF.Sin(jumpProgress * MathF.PI) * 18f;
        const float playerBorder = 2f;
        Vector2 playerScreen = vulkanCamera.WorldToScreen(vulkanPosition, vulkanMap) - new Vector2(0, jumpHeight);
        sprites[tileCount] = new SpritePacket(playerScreen, new(40 + playerBorder * 2, 20 + playerBorder * 2), new(0, 0, 0, 1), default, default, tileCount + 1);
        sprites[tileCount + 1] = new SpritePacket(playerScreen, new(40, 20), new(1, 1, 1, 1), default, default, tileCount + 1);
        renderer.Submit(sprites.AsSpan(0, tileCount + 2));
        renderer.EndFrame();
        Thread.Sleep(2);
    }
}

if (args.Length > 0 && args[0] == "--gdi")
{
    using Win32Window window = new(960, 640, "Isometric Sandbox");
    Win32Input input = new(window.Handle);
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
