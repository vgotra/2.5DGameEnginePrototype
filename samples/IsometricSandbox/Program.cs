using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Platform.Desktop;
using Engine.Rendering;
using Engine.Rendering.Vulkan;
using IsometricSandbox.Game;

bool flatMode = args.Contains("--2d");
bool startFullscreen = args.Contains("--fullscreen");
bool metrics = args.Contains("--metrics");
double frameCap = 0;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--cap" && i + 1 < args.Length && double.TryParse(args[i + 1], out double fps)) frameCap = fps;
}

GameMode mode = flatMode ? GameMode.TopDown : GameMode.Isometric;
const float jumpDuration = 0.24f;
const float playerSpeed = 4f;
const float playerRadius = 0.2f;

using PlatformSession session = GamePlatform.CreateWindow("Archer in the Forest", 800, 600);
IGameWindow window = session.Window;
IInputState input = session.Input;
using VulkanRenderer renderer = new(window.NativeSurface);
TextureLibrary textures = new(renderer);
ArcherGame game = new(new TileMap());
Vector2 position = game.PlayerStart;
Vector2 facing = new(0, 1), jumpStart = position, jumpTarget = position;
float jumpTime = 1f;
IsometricCamera camera = new(window.Size) { Mode = mode };
GameClock clock = new();
FrameTimer frameTimer = new(frameCap);
Vector2 viewport = window.Size;
if (startFullscreen) window.SetFullscreen(true);
int maxSprites = game.Map.Width * game.Map.Height * 2 + 128;
SpritePacket[] sprites = new SpritePacket[maxSprites];
SpritePacket[] spriteScratch = new SpritePacket[maxSprites];
int[] sortKeyCounts = new int[game.Map.Width * game.Map.Height];
Random flicker = new(7);
FrameMetrics frameMetrics = default;
int lastScore = -1;
Vector2 aimTarget = position;
bool pendingShot = false;

while (!window.ShouldClose && !input.IsDown(GameKey.Escape))
{
    long frameStartAlloc = GC.GetAllocatedBytesForCurrentThread();
    long frameStartGen0 = GC.CollectionCount(0);
    long frameStartGen1 = GC.CollectionCount(1);
    long frameStartGen2 = GC.CollectionCount(2);
    window.PumpEvents();
    input.Update();
    if (input.WasPressed(GameKey.Fullscreen)) window.SetFullscreen(!window.Fullscreen);
    if (input.WasPressed(GameKey.Restart))
    {
        game.Reset();
        position = game.PlayerStart;
        facing = new(0, 1);
        jumpStart = position;
        jumpTarget = position;
        jumpTime = 1f;
    }
    if (window.IsMinimized)
    {
        frameTimer.WaitForNextFrame();
        if (frameCap <= 0) Thread.Sleep(15);
        continue;
    }
    if (window.Size != viewport)
    {
        viewport = window.Size;
        camera.Resize(viewport);
        renderer.Resize((int)viewport.X, (int)viewport.Y);
    }
    double elapsed = frameTimer.Advance();
    double frameMs = elapsed * 1000.0;
    clock.Advance(elapsed);
    Vector2 direction = new((input.IsDown(GameKey.Right) ? 1 : 0) - (input.IsDown(GameKey.Left) ? 1 : 0), (input.IsDown(GameKey.Down) ? 1 : 0) - (input.IsDown(GameKey.Up) ? 1 : 0));
    if (input.MousePressed) { aimTarget = camera.ScreenToWorld(input.MousePosition, game.Map); pendingShot = true; }
    int fixedSteps = 0;
    while (clock.TryConsumeFixedStep())
    {
        fixedSteps++;
        if (direction.LengthSquared() > 0) facing = Vector2.Normalize(direction);
        if (input.WasPressed(GameKey.Space) && jumpTime >= jumpDuration)
        {
            Vector2 candidate = position + facing * 2f;
            if (game.Map.CanOccupy(candidate, playerRadius)) { jumpStart = position; jumpTarget = candidate; jumpTime = 0; }
        }
        if (jumpTime < jumpDuration)
        {
            jumpTime = Math.Min(jumpDuration, jumpTime + (float)GameClock.FixedStep);
            position = Vector2.Lerp(jumpStart, jumpTarget, jumpTime / jumpDuration);
        }
        else position = MovementSystem.Move(game.Map, position, direction, playerSpeed, playerRadius, (float)GameClock.FixedStep);
        if (pendingShot) { game.Shoot(position, aimTarget); pendingShot = false; }
        game.UpdateFixed(position, (float)GameClock.FixedStep);
    }
    camera.Follow(position, game.Map);
    renderer.BeginFrame(viewport);
    float jumpProgress = Math.Clamp(jumpTime / jumpDuration, 0, 1);
    float jumpHeight = jumpProgress >= 1 ? 0 : MathF.Sin(jumpProgress * MathF.PI) * 18f;
    int spriteCount = RenderExtractionSystem.ExtractScene(
        game.Map, camera, sprites, game.Animals, game.Arrows.AsSpan(0, game.ArrowCount),
        position, jumpHeight, textures, flicker, sortKeyCounts, spriteScratch);
    renderer.Submit(sprites.AsSpan(0, spriteCount));
    renderer.EndFrame();
    if (game.Score != lastScore)
    {
        lastScore = game.Score;
        window.SetTitle($"Archer in the Forest — Score {game.Score}");
    }
    frameTimer.WaitForNextFrame();
    if (metrics)
    {
        frameMetrics.Add(
            frameMs,
            fixedSteps,
            spriteCount,
            GC.GetAllocatedBytesForCurrentThread() - frameStartAlloc,
            GC.CollectionCount(0) - frameStartGen0,
            GC.CollectionCount(1) - frameStartGen1,
            GC.CollectionCount(2) - frameStartGen2);
    }
}
