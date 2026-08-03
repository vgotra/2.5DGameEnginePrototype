using System.Diagnostics;
using System.Numerics;
using Engine.Core;
using Engine.Ecs;
using Engine.Mathematics;
using Engine.Threading;
using IsometricSandbox.Game;
using Engine.Rendering;

Vector2 original = new(7, 2);
Vector2 projected = IsometricMath.WorldToScreen(original, 64, 32);
Vector2 restored = IsometricMath.ScreenToWorld(projected, 64, 32);
Assert(MathF.Abs(restored.X - original.X) < 0.001f && MathF.Abs(restored.Y - original.Y) < 0.001f, "isometric conversion");

World world = new();
EntityId entity = world.Create();
world.Storage<TestComponent>().Add(entity, new TestComponent(42));
Assert(world.IsAlive(entity), "entity alive");
Assert(world.Storage<TestComponent>().TryGet(entity, out TestComponent value) && value.Value == 42, "component storage");
world.Destroy(entity);
Assert(!world.IsAlive(entity), "stale entity rejected");
Assert(world.Storage<TestComponent>().Count == 0, "destroy purges component storage");
EntityId recycled = world.Create();
Assert(recycled.Index == entity.Index, "entity index recycled");
world.Storage<TestComponent>().Add(recycled, new TestComponent(7));
Assert(world.Storage<TestComponent>().TryGet(recycled, out TestComponent reread) && reread.Value == 7, "component visible after index recycle");
Assert(!world.Storage<TestComponent>().TryGet(entity, out _), "stale generation rejected after recycle");
TileMap map = new();
Assert(map.IsInside(1, 1) && !map.IsInside(-1, 1), "tile bounds");
Assert(map.IsWalkable(0, 0), "open map");
Vector2 moved = MovementSystem.Move(map, new Vector2(2.5f, 2.5f), new Vector2(1, 1), 1, 0.2f, 1);
Assert(moved.X > 2.5f && moved.Y > 2.5f, "free movement");
Assert(map.CanOccupy(new Vector2(10.5f, 10.5f), 0.2f), "open movement");
IsometricCamera camera = new(new Vector2(800, 600));
camera.Follow(new Vector2(10, 10), map);
Assert(camera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(400, 300), "camera center");
IsometricCamera fullscreenCamera = new(new Vector2(1920, 1080));
fullscreenCamera.Follow(new Vector2(2, 2), map);
Assert(fullscreenCamera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(960, 540), "iso map centered in fullscreen viewport");
SpritePacket[] sprites = new SpritePacket[map.Width * map.Height * 2];
int isoExtracted = RenderExtractionSystem.ExtractMapSprites(map, camera, sprites);
Assert(isoExtracted > 0 && isoExtracted <= map.Width * map.Height * 2, "iso map sprite extraction bounded");
Assert(sprites[0].Shape == ShapeKind.Diamond && sprites[1].Shape == ShapeKind.Diamond, "iso sprites are diamonds");
IsometricCamera flatCamera = new(new Vector2(800, 600)) { Isometric = false };
flatCamera.Follow(new Vector2(10, 10), map);
Assert(flatCamera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(400, 300), "flat camera center");
Assert(flatCamera.WorldToScreen(new Vector2(11, 10), map) == new Vector2(464, 300), "flat camera mapping");
IsometricCamera fullscreenFlat = new(new Vector2(1920, 1080)) { Isometric = false };
fullscreenFlat.Follow(new Vector2(2, 2), map);
Assert(fullscreenFlat.WorldToScreen(new Vector2(10, 10), map).X == 960, "flat map centered horizontally in fullscreen viewport");
SpritePacket[] flatSprites = new SpritePacket[map.Width * map.Height * 2];
int flatExtracted = RenderExtractionSystem.ExtractMapSprites(map, flatCamera, flatSprites);
Assert(flatExtracted > 0 && flatExtracted <= map.Width * map.Height * 2, "flat map sprite extraction bounded");
Assert(flatSprites[0].Shape == ShapeKind.Box && flatSprites[1].Shape == ShapeKind.Box && flatSprites[1].Size == new Vector2(map.TileWidth, map.TileWidth), "flat sprites are boxes");
int completed = 0;
using (JobSystem jobs = new(4))
{
    for (int i = 0; i < 100; i++) _ = jobs.Schedule(() => Interlocked.Increment(ref completed));
    await jobs.DrainAsync();
}
Assert(completed == 100, "job system drains all scheduled jobs");

int parallelCompleted = 0;
using (JobSystem parallelJobs = new(8))
{
    for (int i = 0; i < 2000; i++) _ = parallelJobs.Schedule(() => Interlocked.Increment(ref parallelCompleted));
    await parallelJobs.DrainAsync();
}
Assert(parallelCompleted == 2000, "job system drains 2000 jobs across 8 workers");

FrameTimer uncappedTimer = new();
Assert(uncappedTimer.Advance() >= 0, "uncapped frame timer advances");
Stopwatch sw = Stopwatch.StartNew();
uncappedTimer.WaitForNextFrame();
sw.Stop();
Assert(sw.ElapsedMilliseconds < 5, "uncapped wait returns immediately");
FrameTimer cappedTimer = new(60);
cappedTimer.Advance();
sw.Restart();
cappedTimer.WaitForNextFrame();
sw.Stop();
Assert(sw.ElapsedMilliseconds is >= 8 and <= 250, "frame cap paces to ~16.7ms");

GameClock stepClock = new();
for (int i = 0; i < 5; i++) stepClock.Advance(GameClock.FixedStep);
int steps = 0;
while (stepClock.TryConsumeFixedStep()) steps++;
Assert(steps == 5, "game clock consumes one fixed step per advance");
GameClock clampedClock = new();
clampedClock.Advance(0.5);
Assert(Math.Abs(clampedClock.DeltaSeconds - 0.25) < 1e-9, "game clock clamps long frames");
Assert(clampedClock.Accumulator <= 0.25, "game clock accumulator bounded");

TileMap slideMap = new();
slideMap.SetTile(3, 3, TileType.Blocked);
Vector2 slid = slideMap.TryMove(new(3.5f, 2.5f), new(4.0f, 3.0f), 0.2f);
Assert(slid.X == 4.0f && slid.Y == 2.5f, "movement slides horizontally around a blocked cell");
Vector2 stopped = slideMap.TryMove(new(3.4f, 2.4f), new(3.4f, 3.0f), 0.2f);
Assert(stopped == new Vector2(3.4f, 2.4f), "blocked cell stops movement");

IsometricCamera flatFit = new(new Vector2(1280, 1280)) { Isometric = false };
flatFit.Follow(new Vector2(2, 2), map);
Assert(flatFit.WorldToScreen(new Vector2(10, 10), map) == new Vector2(640, 640), "flat map centered on both axes when it fits the viewport");

IsometricCamera narrow = new(new Vector2(300, 300)) { Isometric = false };
narrow.Follow(new Vector2(10, 10), map);
SpritePacket[] culledSprites = new SpritePacket[map.Width * map.Height * 2];
int culledCount = RenderExtractionSystem.ExtractMapSprites(map, narrow, culledSprites);
Assert(culledCount > 0 && culledCount < map.Width * map.Height * 2, "viewport culling skips off-screen tiles");

World multiStore = new();
EntityId multiEntity = multiStore.Create();
multiStore.Storage<TestComponent>().Add(multiEntity, new TestComponent(1));
multiStore.Storage<TestComponent2>().Add(multiEntity, new TestComponent2(2));
Assert(multiStore.Storage<TestComponent>().Count == 1 && multiStore.Storage<TestComponent2>().Count == 1, "two component stores populated");
multiStore.Destroy(multiEntity);
Assert(multiStore.Storage<TestComponent>().Count == 0 && multiStore.Storage<TestComponent2>().Count == 0, "destroy purges every component store");

Console.WriteLine("Smoke tests passed");

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Failed: {name}");
}

public readonly record struct TestComponent(int Value);
public readonly record struct TestComponent2(int Value);
