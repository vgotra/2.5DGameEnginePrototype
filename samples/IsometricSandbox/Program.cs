using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Platform.Desktop;
using Engine.Rendering;
using Engine.Rendering.Vulkan;
using IsometricSandbox.Game;

bool flatMode = args.Contains("--2d");
bool startFullscreen = args.Contains("--fullscreen");
double frameCap = 0;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--cap" && double.TryParse(args[i + 1], out double fps)) frameCap = fps;
}

const float jumpDuration = 0.24f;
const float playerBorder = RenderExtractionSystem.BorderWidth;
Vector2 playerSize = flatMode ? new(40, 40) : new(40, 20);
Vector2 playerBorderSize = playerSize + new Vector2(playerBorder * 2, playerBorder * 2);
ShapeKind playerShape = flatMode ? ShapeKind.Box : ShapeKind.Diamond;
Vector4 black = new(0, 0, 0, 1);
Vector4 white = new(1, 1, 1, 1);

using PlatformSession session = GamePlatform.CreateWindow("Isometric Sandbox", 800, 600);
IGameWindow window = session.Window;
IInputState input = session.Input;
using VulkanRenderer renderer = new(window.NativeSurface);
TileMap map = new();
Vector2 position = map.TileToWorld(2, 2);
Vector2 facing = new(0, 1), jumpStart = position, jumpTarget = position;
float jumpTime = 1f;
IsometricCamera camera = new(window.Size) { Isometric = !flatMode };
GameClock clock = new();
FrameTimer frameTimer = new(frameCap);
Vector2 viewport = window.Size;
if (startFullscreen) window.SetFullscreen(true);
SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2 + 2];
while (!window.ShouldClose && !input.IsDown(GameKey.Escape))
{
    window.PumpEvents();
    input.Update();
    if (input.WasPressed(GameKey.Fullscreen)) window.SetFullscreen(!window.Fullscreen);
    if (window.Size != viewport)
    {
        viewport = window.Size;
        camera.Resize(viewport);
        renderer.Resize((int)viewport.X, (int)viewport.Y);
    }
    clock.Advance(frameTimer.Advance());
    Vector2 direction = new((input.IsDown(GameKey.Right) ? 1 : 0) - (input.IsDown(GameKey.Left) ? 1 : 0), (input.IsDown(GameKey.Down) ? 1 : 0) - (input.IsDown(GameKey.Up) ? 1 : 0));
    while (clock.TryConsumeFixedStep())
    {
        if (direction.LengthSquared() > 0) facing = Vector2.Normalize(direction);
        if (input.WasPressed(GameKey.Space) && jumpTime >= jumpDuration)
        {
            Vector2 candidate = position + facing * 2f;
            if (map.CanOccupy(candidate, 0.2f)) { jumpStart = position; jumpTarget = candidate; jumpTime = 0; }
        }
        if (jumpTime < jumpDuration)
        {
            jumpTime = Math.Min(jumpDuration, jumpTime + (float)GameClock.FixedStep);
            position = Vector2.Lerp(jumpStart, jumpTarget, jumpTime / jumpDuration);
        }
        else position = MovementSystem.Move(map, position, direction, 4, 0.2f, (float)GameClock.FixedStep);
    }
    camera.Follow(position, map);
    renderer.BeginFrame(viewport);
    int tileCount = RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
    float jumpProgress = Math.Clamp(jumpTime / jumpDuration, 0, 1);
    float jumpHeight = jumpProgress >= 1 ? 0 : MathF.Sin(jumpProgress * MathF.PI) * 18f;
    Vector2 playerScreen = camera.WorldToScreen(position, map) - new Vector2(0, jumpHeight);
    sprites[tileCount] = new SpritePacket(playerScreen, playerBorderSize, black, default, default, tileCount + 1, playerShape);
    sprites[tileCount + 1] = new SpritePacket(playerScreen, playerSize, white, default, default, tileCount + 1, playerShape);
    renderer.Submit(sprites.AsSpan(0, tileCount + 2));
    renderer.EndFrame();
    frameTimer.WaitForNextFrame();
}
