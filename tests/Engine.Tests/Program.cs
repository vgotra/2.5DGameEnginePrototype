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
Assert(RenderExtractionSystem.ExtractMapSprites(map, camera, sprites) == map.Width * map.Height * 2, "map sprite extraction");
Assert(sprites[0].Shape == ShapeKind.Diamond && sprites[1].Shape == ShapeKind.Diamond, "iso sprites are diamonds");
IsometricCamera flatCamera = new(new Vector2(800, 600)) { Isometric = false };
flatCamera.Follow(new Vector2(10, 10), map);
Assert(flatCamera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(400, 300), "flat camera center");
Assert(flatCamera.WorldToScreen(new Vector2(11, 10), map) == new Vector2(464, 300), "flat camera mapping");
IsometricCamera fullscreenFlat = new(new Vector2(1920, 1080)) { Isometric = false };
fullscreenFlat.Follow(new Vector2(2, 2), map);
Assert(fullscreenFlat.WorldToScreen(new Vector2(10, 10), map).X == 960, "flat map centered horizontally in fullscreen viewport");
SpritePacket[] flatSprites = new SpritePacket[map.Width * map.Height * 2];
Assert(RenderExtractionSystem.ExtractMapSprites(map, flatCamera, flatSprites) == map.Width * map.Height * 2, "flat map sprite extraction");
Assert(flatSprites[0].Shape == ShapeKind.Box && flatSprites[1].Shape == ShapeKind.Box && flatSprites[1].Size == new Vector2(map.TileWidth, map.TileWidth), "flat sprites are boxes");
int completed = 0;
using (JobSystem jobs = new(4))
{
    for (int i = 0; i < 100; i++) _ = jobs.Schedule(() => Interlocked.Increment(ref completed));
    await jobs.DrainAsync();
}
Assert(completed == 100, "job system drains all scheduled jobs");
Console.WriteLine("Smoke tests passed");

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Failed: {name}");
}

public readonly record struct TestComponent(int Value);
